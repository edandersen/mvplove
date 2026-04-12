using mvcmcpmvp.Services;
using mvcmcpmvp.Models;

namespace MvcMcpMvpTests;

public class MvpDataServiceTests
{
    // Helper: creates a service pre-loaded with the given profiles
    private static MvpDataService CreateService(params MvpProfile[] profiles)
        => new MvpDataService(profiles.AsEnumerable());

    // Helper: builds a minimal but useful MvpProfile for tests
    private static MvpProfile MakeProfile(
        string id,
        string name,
        string country = "US",
        int? years = null,
        List<string>? languages = null,
        List<string>? awards = null,
        List<string>? tech = null,
        string? biography = null)
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
            Biography = biography,
        };

    // --- TotalCount ---

    [Fact]
    public void TotalCount_ReturnsCorrectCount()
    {
        var svc = CreateService(MakeProfile("1", "Alice"), MakeProfile("2", "Bob"));
        Assert.Equal(2, svc.TotalCount);
    }

    // --- GetById ---

    [Fact]
    public void GetById_ReturnsProfile_WhenIdExists()
    {
        var svc = CreateService(MakeProfile("abc", "Alice"));
        var result = svc.GetById("abc");
        Assert.NotNull(result);
        Assert.Equal("Alice", result.Name);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenIdDoesNotExist()
    {
        var svc = CreateService(MakeProfile("abc", "Alice"));
        Assert.Null(svc.GetById("xyz"));
    }

    // --- Search: basic ---

    [Fact]
    public void Search_NoFilters_ReturnsAllProfilesAlphabetically()
    {
        var svc = CreateService(
            MakeProfile("2", "Charlie"),
            MakeProfile("1", "Alice"),
            MakeProfile("3", "Bob"));
        var (results, total) = svc.Search(null, [], [], [], null, 1, 100);
        Assert.Equal(3, total);
        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, results.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void Search_QueryMatchesName_CaseInsensitive()
    {
        var svc = CreateService(MakeProfile("1", "Alice"), MakeProfile("2", "Bob"));
        var (results, total) = svc.Search("alice", [], [], [], null, 1, 100);
        Assert.Equal(1, total);
        Assert.Equal("Alice", results[0].Name);
    }

    [Fact]
    public void Search_QueryNoMatch_ReturnsEmpty()
    {
        var svc = CreateService(MakeProfile("1", "Alice"), MakeProfile("2", "Bob"));
        var (results, total) = svc.Search("zzznomatch", [], [], [], null, 1, 100);
        Assert.Equal(0, total);
        Assert.Empty(results);
    }

    [Fact]
    public void Search_QueryMatchesBiography()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", biography: "expert in cloud computing"),
            MakeProfile("2", "Bob"));
        var (results, total) = svc.Search("cloud", [], [], [], null, 1, 100);
        Assert.Equal(1, total);
        Assert.Equal("Alice", results[0].Name);
    }

    [Fact]
    public void Search_AwardFilter_ReturnsOnlyMatchingProfiles()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", awards: ["Azure"]),
            MakeProfile("2", "Bob", awards: ["Developer"]),
            MakeProfile("3", "Charlie", awards: ["Azure", "Developer"]));
        var (results, total) = svc.Search(null, ["Azure"], [], [], null, 1, 100);
        Assert.Equal(2, total);
        Assert.All(results, r => Assert.Contains("Azure", r.AwardCategory));
    }

    [Fact]
    public void Search_CountryFilter_ReturnsOnlyMatchingProfiles()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: "US"),
            MakeProfile("2", "Bob", country: "UK"),
            MakeProfile("3", "Charlie", country: "US"));
        var (results, total) = svc.Search(null, [], ["US"], [], null, 1, 100);
        Assert.Equal(2, total);
        Assert.All(results, r => Assert.Equal("US", r.Country));
    }

    // --- Search: sort ---

    [Fact]
    public void Search_SortYearsDesc_MostYearsFirst()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", years: 3),
            MakeProfile("2", "Bob", years: 10),
            MakeProfile("3", "Charlie", years: 1));
        var (results, _) = svc.Search(null, [], [], [], "years_desc", 1, 100);
        Assert.Equal(new[] { "Bob", "Alice", "Charlie" }, results.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void Search_SortYearsAsc_FewestYearsFirst()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", years: 3),
            MakeProfile("2", "Bob", years: 10),
            MakeProfile("3", "Charlie", years: 1));
        var (results, _) = svc.Search(null, [], [], [], "years_asc", 1, 100);
        Assert.Equal(new[] { "Charlie", "Alice", "Bob" }, results.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void Search_SortCountry_AlphabeticalByCountry()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: "US"),
            MakeProfile("2", "Bob", country: "AU"),
            MakeProfile("3", "Charlie", country: "UK"));
        var (results, _) = svc.Search(null, [], [], [], "country", 1, 100);
        Assert.Equal(new[] { "AU", "UK", "US" }, results.Select(r => r.Country).ToArray());
    }

    [Fact]
    public void Search_SortLangsDesc_MostLanguagesFirst()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", languages: ["English", "French"]),
            MakeProfile("2", "Bob", languages: ["English", "Spanish", "German"]),
            MakeProfile("3", "Charlie", languages: ["English"]));
        var (results, _) = svc.Search(null, [], [], [], "langs_desc", 1, 100);
        Assert.Equal(new[] { "Bob", "Alice", "Charlie" }, results.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void Search_SortLangsAsc_FewestLanguagesFirst()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", languages: ["English", "French"]),
            MakeProfile("2", "Bob", languages: ["English", "Spanish", "German"]),
            MakeProfile("3", "Charlie", languages: ["English"]));
        var (results, _) = svc.Search(null, [], [], [], "langs_asc", 1, 100);
        Assert.Equal(new[] { "Charlie", "Alice", "Bob" }, results.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void Search_SortLangsDesc_TiesBreakByName()
    {
        var svc = CreateService(
            MakeProfile("1", "Zara", languages: ["English", "French"]),
            MakeProfile("2", "Alice", languages: ["Spanish", "German"]));
        var (results, _) = svc.Search(null, [], [], [], "langs_desc", 1, 100);
        // Both have 2 languages — tie broken alphabetically by name
        Assert.Equal(new[] { "Alice", "Zara" }, results.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void Search_SortLangsAsc_ZeroLanguagesProfilesFirst()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", languages: ["English", "French"]),
            MakeProfile("2", "Bob", languages: []));
        var (results, _) = svc.Search(null, [], [], [], "langs_asc", 1, 100);
        Assert.Equal("Bob", results[0].Name);
        Assert.Equal("Alice", results[1].Name);
    }

    // --- Search: pagination ---

    [Fact]
    public void Search_Pagination_Page1ReturnsFirstPage()
    {
        var profiles = Enumerable.Range(1, 5)
            .Select(i => MakeProfile(i.ToString(), $"Person{i:D2}"))
            .ToArray();
        var svc = CreateService(profiles);
        var (results, total) = svc.Search(null, [], [], [], null, 1, 2);
        Assert.Equal(5, total);
        Assert.Equal(2, results.Count);
        Assert.Equal("Person01", results[0].Name);
        Assert.Equal("Person02", results[1].Name);
    }

    [Fact]
    public void Search_Pagination_Page2ReturnsSecondPage()
    {
        var profiles = Enumerable.Range(1, 5)
            .Select(i => MakeProfile(i.ToString(), $"Person{i:D2}"))
            .ToArray();
        var svc = CreateService(profiles);
        var (results, total) = svc.Search(null, [], [], [], null, 2, 2);
        Assert.Equal(5, total);
        Assert.Equal(2, results.Count);
        Assert.Equal("Person03", results[0].Name);
        Assert.Equal("Person04", results[1].Name);
    }

    [Fact]
    public void Search_Pagination_LastPageReturnsRemainder()
    {
        var profiles = Enumerable.Range(1, 5)
            .Select(i => MakeProfile(i.ToString(), $"Person{i:D2}"))
            .ToArray();
        var svc = CreateService(profiles);
        var (results, total) = svc.Search(null, [], [], [], null, 3, 2);
        Assert.Equal(5, total);
        Assert.Single(results);
        Assert.Equal("Person05", results[0].Name);
    }

    // --- GetTopPolyglots ---

    [Fact]
    public void GetTopPolyglots_ReturnsSortedByLanguageCountDescending()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", languages: ["English"]),
            MakeProfile("2", "Bob", languages: ["English", "French", "Spanish"]),
            MakeProfile("3", "Charlie", languages: ["English", "German"]));
        var result = svc.GetTopPolyglots(10);
        Assert.Equal(new[] { "Bob", "Charlie", "Alice" }, result.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void GetTopPolyglots_ExcludesProfilesWithNoLanguages()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", languages: ["English"]),
            MakeProfile("2", "Bob", languages: []));
        var result = svc.GetTopPolyglots(10);
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public void GetTopPolyglots_RespectsCountParameter()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", languages: ["English"]),
            MakeProfile("2", "Bob", languages: ["English", "French"]),
            MakeProfile("3", "Charlie", languages: ["English", "French", "Spanish"]));
        var result = svc.GetTopPolyglots(2);
        Assert.Equal(2, result.Count);
    }
}
