using System.ClientModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using mvcmcpmvp.Services;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<MvpDataService>();

builder.Services.AddAGUI();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Block requests that didn't come through the Cloudflare CDN.
// Set CloudflareOriginSecret in Azure App Service → Configuration → App Settings.
// Leave empty in development to skip the check.
var cfSecret = app.Configuration["CloudflareOriginSecret"];
if (!string.IsNullOrWhiteSpace(cfSecret))
{
    app.Use(async (context, next) =>
    {
        if (!context.Request.Headers.TryGetValue("Secret", out var value) || value != cfSecret)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden");
            return;
        }
        await next(context);
    });
}



// create the MVP Copilot agent
if (!string.IsNullOrWhiteSpace(app.Configuration["OpenAIApiKey"]))
{

    var deploymentName = app.Configuration["OpenAIModelName"] ?? "gpt-4o-mini";
    var apiKey = app.Configuration["OpenAIApiKey"];

    OpenAIClient client = new OpenAIClient(apiKey);

    var chatClient = client.GetChatClient(deploymentName);
    var agent = chatClient.AsIChatClient()
    .AsAIAgent(
        instructions: "You are a helpful assistant that answers questions about Microsoft MVPs and ONLY Microsoft MVPs. " + 
        "Do NOT help the user with anything not related to MVPs, politely decline to help.", name: "agentic_chat",
        tools: [AIFunctionFactory.Create(typeof(AgentTools).GetMethod("SearchMVPs")!, 
        new AgentTools(app.Services.GetRequiredService<MvpDataService>()))]);

    app.MapAGUI("/mvpcopilot", agent);
}

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
