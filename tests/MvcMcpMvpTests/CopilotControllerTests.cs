using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using mvcmcpmvp.Controllers;
using System.Collections.Generic;
using Xunit;

namespace MvcMcpMvpTests;

public class CopilotControllerTests
{
    // Helper to create a controller with a given configuration dictionary
    private static CopilotController CreateController(IDictionary<string, string>? values = null)
    {
        var builder = new ConfigurationBuilder();
        if (values != null)
        {
            builder.AddInMemoryCollection(values);
        }
        var config = builder.Build();
        return new CopilotController(config);
    }

    [Fact]
    public void Index_ReturnsViewResult()
    {
        var controller = CreateController();
        var result = controller.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Index_ModelIsTrue_WhenApiKeyConfigured()
    {
        var controller = CreateController(new Dictionary<string, string>
        {
            ["OpenAIApiKey"] = "dummy-key"
        });
        var result = controller.Index() as ViewResult;
        Assert.NotNull(result);
        Assert.IsType<bool>(result!.Model);
        Assert.True((bool)result.Model!);
    }

    [Fact]
    public void Index_ModelIsFalse_WhenApiKeyMissingOrEmpty()
    {
        // Missing key
        var controllerMissing = CreateController();
        var resultMissing = controllerMissing.Index() as ViewResult;
        Assert.NotNull(resultMissing);
        Assert.False((bool)resultMissing!.Model!);

        // Empty string key
        var controllerEmpty = CreateController(new Dictionary<string, string>
        {
            ["OpenAIApiKey"] = string.Empty
        });
        var resultEmpty = controllerEmpty.Index() as ViewResult;
        Assert.NotNull(resultEmpty);
        Assert.False((bool)resultEmpty!.Model!);
    }

}
