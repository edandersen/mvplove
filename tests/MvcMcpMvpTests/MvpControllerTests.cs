using Microsoft.AspNetCore.Mvc;
using mvcmcpmvp.Models;
using mvcmcpmvp.Services;
using mvcmcpmvp.Controllers;

namespace MvcMcpMvpTests;

public class MvpControllerTests
{
    // --- Helpers ---

    private static MvpController CreateController(params MvpProfile[] profiles)
    {
        var svc = new MvpDataService(profiles.AsEnumerable());
        return new MvpController(svc);
    }

    private static MvpProfile Make(
        string id,
        string name,
        string country = "US",
        int? years = null,
        List<string>? languages = null,
        List<string>? awards = null,
        List<string>? tech = null)
        => new MvpProfile
        {
            Id = id,
            Name = name,
            Country = country,
            Headline = $"{name} headline",
            YearsInProgram = years,
            Languages = languages ?? [],
            AwardCategory = awards ?? [],
            TechnologyFocusArea = tech ?? [],
        };

    // --- Index action ---

    [Fact]
    public void Index_ReturnsViewResult()
    {
        var controller = CreateController(Make("1", "Alice"));
        var result = controller.Index(null, [], [], [], null, 1);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Index_ReturnsViewModelWithCorrectData()
    {
         // 50 profiles: 25 Azure (odd) and 25 Developer (even)
         // Search for Azure → 25 results, page 1 returns 24
         var profiles = Enumerable.Range(1, 50)
             .Select(i => Make(i.ToString(), $"Person{i:D2}", awards: i % 2 == 1 ? ["Azure"] : ["Developer"]))
             .ToArray();
         var controller = CreateController(profiles);

         var result = controller.Index(null, ["Azure"], [], [], null, 1) as ViewResult;

         Assert.NotNull(result);
         var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);
         Assert.Equal(25, vm.TotalResults); // 25 out of 50 have Azure award
         Assert.Equal(50, vm.TotalMvps);
         Assert.Equal(24, vm.Results.Count); // page size 24, page 1
         Assert.All(vm.Results, r => Assert.Contains("Azure", r.AwardCategory));
     }

    [Fact]
    public void Index_PassesQueryToViewModel()
    {
        var controller = CreateController(Make("1", "Alice"));
        var result = controller.Index("Alice", [], [], [], null, 1) as ViewResult;
        var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);
        Assert.Equal("Alice", vm.Query);
    }

    [Fact]
    public void Index_PassesSelectedFiltersToViewModel()
    {
        var controller = CreateController(
            Make("1", "Alice", awards: ["Azure"]),
            Make("2", "Bob", country: "UK"),
            Make("3", "Charlie", tech: ["Azure"]));

        var result = controller.Index(
            q: null,
            awards: ["Azure"],
            countries: ["UK"],
            tech: ["Azure"],
            sort: null,
            page: 1) as ViewResult;

        var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);
         Assert.Single(vm.SelectedAwards); // ["Azure"] passed in
         Assert.Single(vm.SelectedCountries);
         Assert.Single(vm.SelectedTech);
    }

    [Fact]
    public void Index_SearchReturnsOnlyMatchingProfiles()
    {
        var controller = CreateController(
            Make("1", "Alice", awards: ["Azure"]),
            Make("2", "Bob", awards: ["Developer"]),
            Make("3", "Charlie", awards: ["Azure"]));

        var result = controller.Index(null, ["Azure"], [], [], null, 1) as ViewResult;
        var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);

        Assert.Equal(2, vm.TotalResults);
        Assert.All(vm.Results, r => Assert.Contains("Azure", r.AwardCategory));
    }

    [Fact]
    public void Index_HandlesPageNumber()
    {
          // Controller uses page size 24, so need 25 for page 2 to have 1 item
          var profiles = Enumerable.Range(1, 25)
              .Select(i => Make(i.ToString(), $"Person{i:D2}"))
              .ToArray();
          var controller = CreateController(profiles);

          var result = controller.Index(null, [], [], [], null, 2) as ViewResult;
          var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);

          Assert.Equal(25, vm.TotalResults);
          Assert.Equal(2, vm.Page);
          Assert.Equal(24, vm.PageSize); // controller default
          Assert.Single(vm.Results); // only Person25 on page 2
          Assert.Equal("Person25", vm.Results[0].Name);
      }

    [Fact]
    public void Index_PassesPageToViewModel()
    {
        var controller = CreateController(Make("1", "Alice"));
        var result = controller.Index(null, [], [], [], null, 3) as ViewResult;
        var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);
        Assert.Equal(3, vm.Page);
    }

    [Fact]
    public void Index_PassesTotalPagesToViewModel()
    {
        var profiles = Enumerable.Range(1, 7)
            .Select(i => Make(i.ToString(), $"Person{i:D2}"))
            .ToArray();
        var controller = CreateController(profiles);

        var result = controller.Index(null, [], [], [], null, 1) as ViewResult;
        var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);

        // 7 results / 24 page size = 1 page
        Assert.Equal(1, vm.TotalPages);
    }

    [Fact]
    public void Index_HasFiltersTrue_WhenQueryProvided()
    {
        var controller = CreateController(Make("1", "Alice"));
        var result = controller.Index("test", [], [], [], null, 1) as ViewResult;
        var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);
        Assert.True(vm.HasFilters);
    }

    [Fact]
    public void Index_HasFiltersTrue_WhenAwardsSelected()
    {
        var controller = CreateController(Make("1", "Alice"));
        var result = controller.Index(null, ["Azure"], [], [], null, 1) as ViewResult;
        var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);
        Assert.True(vm.HasFilters);
    }

    [Fact]
    public void Index_HasFiltersFalse_WhenNoFilters()
    {
        var controller = CreateController(Make("1", "Alice"));
        var result = controller.Index(null, [], [], [], null, 1) as ViewResult;
        var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);
        Assert.False(vm.HasFilters);
    }

    [Fact]
    public void Index_PopulatesFilterOptions()
    {
        var controller = CreateController(
            Make("1", "Alice", awards: ["Azure"], country: "US"),
            Make("2", "Bob", awards: ["Developer"], country: "UK"));

        var result = controller.Index(null, [], [], [], null, 1) as ViewResult;
        var vm = Assert.IsType<MvpBrowseViewModel>(result.Model);

        Assert.NotEmpty(vm.AwardOptions);
        Assert.Equal(2, vm.AwardOptions.Count); // Azure + Developer
        Assert.NotEmpty(vm.CountryOptions);
        Assert.Equal(2, vm.CountryOptions.Count); // US + UK
    }

    // --- Grid action (HTMX partial) ---

    [Fact]
    public void Grid_ReturnsPartialViewResult()
    {
        var controller = CreateController(Make("1", "Alice"));
        var result = controller.Grid(null, [], [], [], null, 1);
        Assert.IsType<PartialViewResult>(result);
    }

    [Fact]
    public void Grid_ReturnsCorrectPartialViewName()
    {
        var controller = CreateController(Make("1", "Alice"));
        var result = controller.Grid(null, [], [], [], null, 1) as PartialViewResult;
        Assert.Equal("_Grid", result.ViewName);
    }

    [Fact]
    public void Grid_ReturnsGridViewModel()
    {
        var controller = CreateController(Make("1", "Alice"));
        var result = controller.Grid(null, [], [], [], null, 1) as PartialViewResult;
        var vm = Assert.IsType<MvpGridViewModel>(result.Model);
        Assert.Single(vm.Results);
        Assert.Equal("Alice", vm.Results[0].Name);
    }

    [Fact]
    public void Grid_PaginationWorks()
    {
           // Controller uses page size 24, so need 25 for page 2 to have 1 item
           var profiles = Enumerable.Range(1, 25)
               .Select(i => Make(i.ToString(), $"Person{i:D2}"))
               .ToArray();
           var controller = CreateController(profiles);

           var result = controller.Grid(null, [], [], [], null, 2) as PartialViewResult;
           var vm = Assert.IsType<MvpGridViewModel>(result.Model);

           Assert.Equal(2, vm.Page);
           Assert.Single(vm.Results);
           Assert.Equal("Person25", vm.Results[0].Name);
       }

    [Fact]
    public void Grid_SearchFiltersResults()
    {
        var controller = CreateController(
            Make("1", "Alice", awards: ["Azure"]),
            Make("2", "Bob", awards: ["Developer"]));

        var result = controller.Grid(null, ["Azure"], [], [], null, 1) as PartialViewResult;
        var vm = Assert.IsType<MvpGridViewModel>(result.Model);

        Assert.Equal(1, vm.TotalResults);
        Assert.Equal("Alice", vm.Results[0].Name);
    }

    // --- Detail action ---

    [Fact]
    public void Detail_ReturnsViewResult_WhenMvpExists()
    {
        var controller = CreateController(Make("abc", "Alice"));
        var result = controller.Detail("abc");
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Detail_ReturnsCorrectMvpInViewModel()
    {
        var controller = CreateController(
            Make("abc", "Alice", country: "US"),
            Make("xyz", "Bob", country: "UK"));

        var result = controller.Detail("abc") as ViewResult;
        var mvp = Assert.IsType<MvpProfile>(result.Model);
        Assert.Equal("Alice", mvp.Name);
        Assert.Equal("US", mvp.Country);
    }

    [Fact]
    public void Detail_ReturnsNotFound_WhenMvpDoesNotExist()
    {
        var controller = CreateController(Make("abc", "Alice"));
        var result = controller.Detail("zzz");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Detail_ReturnsNotFound_WhenIdIsNullOrEmpty()
    {
        var controller = CreateController(Make("abc", "Alice"));

        var resultNull = controller.Detail(null!);
        Assert.IsType<NotFoundResult>(resultNull);

        var resultEmpty = controller.Detail("");
        Assert.IsType<NotFoundResult>(resultEmpty);
    }
}
