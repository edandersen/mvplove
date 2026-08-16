using System.ClientModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using AspNetCoreMcpServer.Tools;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using mvcmcpmvp.Services;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using AgentGovernance.Integration;
using AgentGovernance.Policy;
using AgentGovernance;
using AgentGovernance.Extensions.Microsoft.Agents;
using System.Text.Json;
using AgentGovernance.Extensions.ModelContextProtocol;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<MvpDataService>();

builder.Services.AddAGUI();

builder.Services.AddMcpServer()
 .WithGovernance(options =>
    {
        options.PolicyPaths.Add("policies/default.yaml");
        options.DefaultAgentId = "did:mesh:default";
        options.EnablePromptInjectionDetection = true;
        options.RequireAuthenticatedAgentId = false;
        options.EnableAudit = true;
    })
.WithHttpTransport()
.WithTools<MvpMcpTool>();

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

    var config = new GovernanceOptions
    {
        PolicyPaths = new() { "policies/default.yaml" },
        ConflictStrategy = ConflictResolutionStrategy.DenyOverrides,
        EnablePromptInjectionDetection = true,    // Scan inputs for injection attacks
    };

    var kernel = new GovernanceKernel(config);

    kernel.OnAllEvents(evt => Console.WriteLine("AI Governance: " + evt.Type + " " 
    + string.Join(", ", evt.Data.Select(d => d.Key + ": " + JsonSerializer.Serialize(d.Value)))));
    
    var adapter = new AgentFrameworkGovernanceAdapter(
    kernel,
    new AgentFrameworkGovernanceOptions
    {
        DefaultAgentId = "did:mesh:default",
        EnableFunctionMiddleware = true,
    });

    var chatClient = client.GetChatClient(deploymentName);
    var agent = chatClient.AsIChatClient()
    .AsAIAgent(
        instructions: "You are a helpful assistant that answers questions about Microsoft MVPs and ONLY Microsoft MVPs. " + 
        "Do NOT help the user with anything not related to MVPs, politely decline to help.", name: "agentic_chat",
        tools: [AIFunctionFactory.Create(typeof(AgentTools).GetMethod("SearchMVPs")!, 
        new AgentTools(app.Services.GetRequiredService<MvpDataService>()))]);

    var governedAgent = agent
    .AsBuilder()
    .WithGovernance(adapter)
    .Build();

    var safeAgent = new PromptInjectionDetectionAgent(governedAgent, kernel);

    app.MapAGUI("/mvpcopilot", safeAgent);
}

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapMcp("/mcp");

app.Run();
