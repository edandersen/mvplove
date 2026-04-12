using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using mvcmcpmvp.Models;
using mvcmcpmvp.Services;

namespace mvcmcpmvp.Controllers;

public class HomeController : Controller
{
    private readonly MvpDataService _svc;

    public HomeController(MvpDataService svc) => _svc = svc;

    public IActionResult Index()
    {
        var vm = new HomeDashboardViewModel
        {
            LongestTenured = _svc.GetTopTenured(),
            MostLanguages  = _svc.GetTopPolyglots(),
            NewMvps        = _svc.GetRandomNewMvps(),
            TotalCount     = _svc.TotalCount,
        };
        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
