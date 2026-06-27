using Microsoft.AspNetCore.Mvc;
using mvcmcpmvp.Models;
using mvcmcpmvp.Services;
using mvcmcpmvp.Controllers;

namespace MvcMcpMvpTests;

public class HomeControllerTests
{
    private static HomeController CreateController(params MvpProfile[] profiles)
    {
        var svc = new MvpDataService(profiles.AsEnumerable());
        return new HomeController(svc);
    }

     private static MvpProfile MakeProfile(string id, string name)
         => new MvpProfile { Id = id, Name = name, Country = "US", Headline = $"{name} headline", YearsInProgram = null, Languages = [], AwardCategory = [], TechnologyFocusArea = [], Biography = null };

    // --- Random action ---

    [Fact]
    public void Random_ReturnsRedirectToAction()
    {
        var controller = CreateController(
            MakeProfile("abc", "Alice"),
            MakeProfile("xyz", "Bob"));

        var result = controller.Random();

        var redirectTo = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirectTo.ActionName);
        Assert.Equal("Mvp", redirectTo.ControllerName);
    }

    [Fact]
    public void Random_DestinationHasValidMvpId()
    {
        var controller = CreateController(
            MakeProfile("aaa", "Alice"),
            MakeProfile("bbb", "Bob"),
            MakeProfile("ccc", "Charlie"));

        var result = controller.Random();

        var redirectTo = Assert.IsType<RedirectToActionResult>(result);
        var id = Assert.IsType<string>(redirectTo.RouteValues["id"]);
        Assert.Contains(id, new[] { "aaa", "bbb", "ccc" });
    }

    [Fact]
    public void Random_VariesAcrossMultipleCalls()
    {
        var controller = CreateController(
            MakeProfile("1", "Alice"),
            MakeProfile("2", "Bob"),
            MakeProfile("3", "Charlie"),
            MakeProfile("4", "Dave"),
            MakeProfile("5", "Eve"));

        var ids = Enumerable.Range(1, 100)
            .Select(_ =>
            {
                var r = controller.Random();
                var rd = Assert.IsType<RedirectToActionResult>(r);
                return rd.RouteValues["id"] as string;
            })
            .ToHashSet();

        // With 5 profiles and 100 calls, we should see more than 1 unique ID
        Assert.True(ids.Count > 1, "Random action should return different MVPs across multiple calls");
    }
}

public class MvpDataServiceGetRandomIdTests
{
    private static MvpDataService CreateService(params MvpProfile[] profiles)
        => new MvpDataService(profiles.AsEnumerable());

    private static MvpProfile Make(string id, string name)
        => new MvpProfile { Id = id, Name = name, Country = "US", Headline = $"{name} headline", YearsInProgram = null, Languages = [], AwardCategory = [], TechnologyFocusArea = [], Biography = null };

    [Fact]
    public void GetRandomId_ReturnsValidId()
    {
        var svc = CreateService(
            Make("alpha", "Alice"),
            Make("beta", "Bob"));

        var id = svc.GetRandomId();
        Assert.Contains(id, new[] { "alpha", "beta" });
    }

    [Fact]
    public void GetRandomId_ThrowsOnEmptyData()
    {
        var svc = CreateService();

        Assert.Throws<InvalidOperationException>(() => svc.GetRandomId());
    }

    [Fact]
    public void GetRandomId_VariesAcrossMultipleCalls()
    {
        var svc = CreateService(
            Make("1", "Alice"),
            Make("2", "Bob"),
            Make("3", "Charlie"));

        var ids = Enumerable.Range(1, 100).Select(_ => svc.GetRandomId()).ToHashSet();

        Assert.True(ids.Count > 1, "GetRandomId should return different IDs across multiple calls");
    }
}
