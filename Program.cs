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
if (!string.IsNullOrWhiteSpace("OpenAIApiKey"))
{



    var endpoint = app.Configuration["OpenAIEndpoint"];
    var deploymentName = app.Configuration["OpenAIModelName"] ?? "gpt-4o-mini";
    var apiKey = app.Configuration["OpenAIApiKey"];

OpenAIClient client = new OpenAIClient(apiKey);
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    var chatClient = client.GetChatClient(deploymentName);
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    // var agent2 = chatClient.AsIChatClient().AsAIAgent(
    //         name: "AgenticChat",
    //         description: "A simple chat agent using Azure OpenAI");
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    var agent = chatClient.AsIChatClient()
    .AsAIAgent(
        instructions: "You are a helpful assistant.", name: "agentic_chat");
        //tools: [AIFunctionFactory.Create(GetWeather)]);

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
