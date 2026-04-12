using Microsoft.AspNetCore.Mvc;

namespace mvcmcpmvp.Controllers;

public class CopilotController : Controller
{
    private readonly IConfiguration _config;

    public CopilotController(IConfiguration config) => _config = config;

    public IActionResult Index()
    {
        var hasApiKey = !string.IsNullOrEmpty(_config["OpenAIApiKey"]);
        return View(hasApiKey);
    }
}
