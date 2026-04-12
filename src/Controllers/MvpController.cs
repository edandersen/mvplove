using Microsoft.AspNetCore.Mvc;
using mvcmcpmvp.Models;
using mvcmcpmvp.Services;

namespace mvcmcpmvp.Controllers;

public class MvpController : Controller
{
    private readonly MvpDataService _svc;

    public MvpController(MvpDataService svc) => _svc = svc;

    public IActionResult Index(
        string? q,
        [FromQuery(Name = "award")]   List<string>? awards,
        [FromQuery(Name = "country")] List<string>? countries,
        [FromQuery(Name = "tech")]    List<string>? tech,
        string? sort,
        int page = 1)
    {
        awards    ??= [];
        countries ??= [];
        tech      ??= [];

        var (results, total) = _svc.Search(q, awards, countries, tech, sort, page, 24);

        var vm = new MvpBrowseViewModel
        {
            Query            = q,
            SelectedAwards   = awards,
            SelectedCountries = countries,
            SelectedTech     = tech,
            Sort             = sort,
            Results          = results,
            TotalResults     = total,
            TotalMvps        = _svc.TotalCount,
            Page             = page,
            PageSize         = 24,
            AwardOptions     = _svc.GetAwardOptions(),
            CountryOptions   = _svc.GetCountryOptions(),
            TechOptions      = _svc.GetTechOptions(),
        };

        return View(vm);
    }

    // HTMX partial endpoint — returns only the cards + pagination
    public IActionResult Grid(
        string? q,
        [FromQuery(Name = "award")]   List<string>? awards,
        [FromQuery(Name = "country")] List<string>? countries,
        [FromQuery(Name = "tech")]    List<string>? tech,
        string? sort,
        int page = 1)
    {
        awards    ??= [];
        countries ??= [];
        tech      ??= [];

        var (results, total) = _svc.Search(q, awards, countries, tech, sort, page, 24);

        var vm = new MvpGridViewModel
        {
            Query            = q,
            SelectedAwards   = awards,
            SelectedCountries = countries,
            SelectedTech     = tech,
            Sort             = sort,
            Results          = results,
            TotalResults     = total,
            TotalMvps        = _svc.TotalCount,
            Page             = page,
            PageSize         = 24,
        };

        return PartialView("_Grid", vm);
    }

    public IActionResult Detail(string id)
    {
        var mvp = _svc.GetById(id);
        if (mvp is null) return NotFound();
        return View(mvp);
    }
}
