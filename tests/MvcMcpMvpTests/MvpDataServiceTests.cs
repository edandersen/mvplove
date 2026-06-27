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

    // --- GetTopTenured ---

    [Fact]
    public void GetTopTenured_ReturnsSortedByYearsDescending()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", years: 3),
            MakeProfile("2", "Bob", years: 10),
            MakeProfile("3", "Charlie", years: 5));
        var result = svc.GetTopTenured(10);
        Assert.Equal(new[] { "Bob", "Charlie", "Alice" }, result.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void GetTopTenured_ExcludesProfilesWithNoYears()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", years: 5),
            MakeProfile("2", "Bob", years: null));
        var result = svc.GetTopTenured(10);
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public void GetTopTenured_RespectsCountParameter()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", years: 1),
            MakeProfile("2", "Bob", years: 2),
            MakeProfile("3", "Charlie", years: 3));
        var result = svc.GetTopTenured(2);
        Assert.Equal(2, result.Count);
        Assert.Equal("Charlie", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
    }

    [Fact]
    public void GetTopTenured_TiesBreakByName()
    {
        var svc = CreateService(
            MakeProfile("1", "Zara", years: 5),
            MakeProfile("2", "Alice", years: 5));
        var result = svc.GetTopTenured(10);
        Assert.Equal(new[] { "Alice", "Zara" }, result.Select(r => r.Name).ToArray());
    }

    // --- GetRandomNewMvps ---

    [Fact]
    public void GetRandomNewMvps_ReturnsOnlyNewMvps()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", years: 1),
            MakeProfile("2", "Bob", years: 3),
            MakeProfile("3", "Charlie", years: 1));
        var result = svc.GetRandomNewMvps(10);
        Assert.All(result, r => Assert.Equal(1, r.YearsInProgram));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetRandomNewMvps_RespectsCountParameter()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", years: 1),
            MakeProfile("2", "Bob", years: 1),
            MakeProfile("3", "Charlie", years: 1));
        var result = svc.GetRandomNewMvps(2);
        Assert.Equal(2, result.Count);
    }

    // --- GetAwardOptions ---

    [Fact]
    public void GetAwardOptions_ReturnsAllAwardsWithCounts()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", awards: ["Azure", "Developer"]),
            MakeProfile("2", "Bob", awards: ["Azure"]),
            MakeProfile("3", "Charlie", awards: ["Developer", "Microsoft AI"]));
        var result = svc.GetAwardOptions();
        Assert.Equal(3, result.Count);
        Assert.Equal("Azure", result[0].Value);
        Assert.Equal(2, result[0].Count);
    }

    [Fact]
    public void GetAwardOptions_FilteredByCountries()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", awards: ["Azure"], country: "US"),
            MakeProfile("2", "Bob", awards: ["Developer"], country: "UK"),
            MakeProfile("3", "Charlie", awards: ["Microsoft AI"], country: "US"));
        var result = svc.GetAwardOptions(countries: ["US"]);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.NotEqual("Developer", r.Value));
    }

    [Fact]
    public void GetAwardOptions_FilteredByTech()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", awards: ["Azure"], tech: ["Azure"]),
            MakeProfile("2", "Bob", awards: ["Developer"], tech: ["GitHub"]),
            MakeProfile("3", "Charlie", awards: ["Microsoft AI"], tech: ["Azure"]));
        var result = svc.GetAwardOptions(tech: ["Azure"]);
        Assert.All(result, r => Assert.NotEqual("Developer", r.Value));
    }

    // --- GetCountryOptions ---

    [Fact]
    public void GetCountryOptions_ReturnsAllCountriesWithCounts()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: "US"),
            MakeProfile("2", "Bob", country: "UK"),
            MakeProfile("3", "Charlie", country: "US"));
        var result = svc.GetCountryOptions();
        Assert.Equal(2, result.Count);
        var us = result.First(r => r.Value == "US");
        Assert.Equal(2, us.Count);
    }

    [Fact]
    public void GetCountryOptions_FilteredByAwards()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: "US", awards: ["Azure"]),
            MakeProfile("2", "Bob", country: "UK", awards: ["Developer"]),
            MakeProfile("3", "Charlie", country: "CA", awards: ["Azure"]));
        var result = svc.GetCountryOptions(awards: ["Azure"]);
        Assert.All(result, r => Assert.NotEqual("UK", r.Value));
    }

    [Fact]
    public void GetCountryOptions_FilteredByTech()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: "US", tech: ["Azure"]),
            MakeProfile("2", "Bob", country: "UK", tech: ["GitHub"]),
            MakeProfile("3", "Charlie", country: "CA", tech: ["Azure"]));
        var result = svc.GetCountryOptions(tech: ["Azure"]);
        Assert.All(result, r => Assert.NotEqual("UK", r.Value));
    }

    [Fact]
    public void GetCountryOptions_ExcludesEmptyCountries()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: ""),
            MakeProfile("2", "Bob", country: "US"));
        var result = svc.GetCountryOptions();
        Assert.Single(result);
        Assert.Equal("US", result[0].Value);
    }

    // --- GetTechOptions ---

    [Fact]
    public void GetTechOptions_ReturnsAllTechWithCounts()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", tech: ["Azure", "GitHub"]),
            MakeProfile("2", "Bob", tech: ["GitHub"]),
            MakeProfile("3", "Charlie", tech: ["AI"]));
        var result = svc.GetTechOptions();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetTechOptions_FilteredByAwards()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", tech: ["Azure"], awards: ["Azure"]),
            MakeProfile("2", "Bob", tech: ["GitHub"], awards: ["Developer"]),
            MakeProfile("3", "Charlie", tech: ["AI"], awards: ["Azure"]));
        var result = svc.GetTechOptions(awards: ["Azure"]);
        Assert.All(result, r => Assert.NotEqual("GitHub", r.Value));
    }

    [Fact]
    public void GetTechOptions_FilteredByCountries()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", tech: ["Azure"], country: "US"),
            MakeProfile("2", "Bob", tech: ["GitHub"], country: "UK"),
            MakeProfile("3", "Charlie", tech: ["AI"], country: "US"));
        var result = svc.GetTechOptions(countries: ["US"]);
        Assert.All(result, r => Assert.NotEqual("GitHub", r.Value));
    }

    // --- Collection Properties ---

    [Fact]
    public void Countries_ReturnsDistinctSortedCountries()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: "US"),
            MakeProfile("2", "Bob", country: "UK"),
            MakeProfile("3", "Charlie", country: "AU"),
            MakeProfile("4", "Dave", country: "US"));
        Assert.Equal(new[] { "AU", "UK", "US" }, svc.Countries.ToArray());
    }

    [Fact]
    public void Countries_ExcludesEmptyCountries()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: ""),
            MakeProfile("2", "Bob", country: "US"));
        Assert.Single(svc.Countries);
        Assert.Equal("US", svc.Countries[0]);
    }

    [Fact]
    public void AwardCategories_ReturnsDistinctSorted()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", awards: ["Azure", "Developer"]),
            MakeProfile("2", "Bob", awards: ["Developer", "Microsoft AI"]));
        Assert.Equal(new[] { "Azure", "Developer", "Microsoft AI" }, svc.AwardCategories.ToArray());
    }

    [Fact]
    public void TechFocusAreas_ReturnsDistinctSorted()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", tech: ["Azure", "GitHub"]),
            MakeProfile("2", "Bob", tech: ["GitHub", "AI"]));
        Assert.Equal(new[] { "AI", "Azure", "GitHub" }, svc.TechFocusAreas.ToArray());
    }
}
