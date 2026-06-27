using mvcmcpmvp.Models;
using mvcmcpmvp.Services;

namespace MvcMcpMvpTests;

public class AgentToolsTests
{
    private static MvpDataService CreateService(params MvpProfile[] profiles)
        => new MvpDataService(profiles.AsEnumerable());

    private static MvpProfile MakeProfile(
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

    private static AgentTools CreateTools(params MvpProfile[] profiles)
        => new AgentTools(CreateService(profiles));

    // --- Construction ---

    [Fact]
    public void Constructor_StoresService()
    {
        var svc = CreateService(MakeProfile("1", "Alice"));
        var tools = new AgentTools(svc);
        Assert.NotNull(tools);
    }

    // --- SearchMVPs: all parameters null/empty ---

    [Fact]
    public void SearchMVPs_AllNullFilters_ReturnsAllProfiles()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice"),
            MakeProfile("2", "Bob"),
            MakeProfile("3", "Charlie"));

        var results = tools.SearchMVPs(namePart: null, awards: null, countries: null, tech: null);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void SearchMVPs_AllEmptyStrings_ReturnsAllProfiles()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice"),
            MakeProfile("2", "Bob"));

        var results = tools.SearchMVPs(namePart: "", awards: "", countries: "", tech: "");
        Assert.Equal(2, results.Count);
    }

    // --- SearchMVPs: namePart ---

    [Fact]
    public void SearchMVPs_NamePart_FiltersByName()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice Smith"),
            MakeProfile("2", "Bob Jones"),
            MakeProfile("3", "Alice Johnson"));

        var results = tools.SearchMVPs(namePart: "Alice");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("Alice", r.Name));
    }

    [Fact]
    public void SearchMVPs_NamePart_CaseInsensitive()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice"),
            MakeProfile("2", "Bob"));

        var results = tools.SearchMVPs(namePart: "alice");
        Assert.Single(results);
        Assert.Equal("Alice", results[0].Name);
    }

    [Fact]
    public void SearchMVPs_NamePart_NoMatch_ReturnsEmpty()
    {
        var tools = CreateTools(MakeProfile("1", "Alice"));
        var results = tools.SearchMVPs(namePart: "zzz");
        Assert.Empty(results);
    }

    // --- SearchMVPs: awards ---

    [Fact]
    public void SearchMVPs_Awards_CommaSeparated()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice", awards: ["Azure"]),
            MakeProfile("2", "Bob", awards: ["Developer"]),
            MakeProfile("3", "Charlie", awards: ["Azure", "Developer"]));

        var results = tools.SearchMVPs(awards: "Azure,Developer");
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void SearchMVPs_Awards_SingleValue()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice", awards: ["Azure"]),
            MakeProfile("2", "Bob", awards: ["Developer"]));

        var results = tools.SearchMVPs(awards: "Azure");
        Assert.Single(results);
        Assert.Equal("Alice", results[0].Name);
    }

    [Fact]
    public void SearchMVPs_Awards_EmptyString_TreatedAsNoFilter()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice", awards: ["Azure"]),
            MakeProfile("2", "Bob", awards: ["Developer"]));

        var results = tools.SearchMVPs(awards: "");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void SearchMVPs_Awards_WithLeadingTrailingSpaces_ParsedCorrectly()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice", awards: ["Azure"]),
            MakeProfile("2", "Bob", awards: ["Developer"]));

        var results = tools.SearchMVPs(awards: "  Azure , Developer  ");
        Assert.Equal(2, results.Count);
    }

    // --- SearchMVPs: countries ---

    [Fact]
    public void SearchMVPs_Countries_MultiValueFilter()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice", country: "US"),
            MakeProfile("2", "Bob", country: "UK"),
            MakeProfile("3", "Charlie", country: "AU"));

        var results = tools.SearchMVPs(countries: "US,AU");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void SearchMVPs_Countries_EmptyString_TreatedAsNoFilter()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice", country: "US"),
            MakeProfile("2", "Bob", country: "UK"));

        var results = tools.SearchMVPs(countries: "");
        Assert.Equal(2, results.Count);
    }

    // --- SearchMVPs: tech ---

    [Fact]
    public void SearchMVPs_Tech_MultiValueFilter()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice", tech: ["Azure"]),
            MakeProfile("2", "Bob", tech: ["GitHub"]),
            MakeProfile("3", "Charlie", tech: ["AI"]));

        var results = tools.SearchMVPs(tech: "Azure,AI");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void SearchMVPs_Tech_EmptyString_TreatedAsNoFilter()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice", tech: ["Azure"]),
            MakeProfile("2", "Bob", tech: ["GitHub"]));

        var results = tools.SearchMVPs(tech: "");
        Assert.Equal(2, results.Count);
    }

    // --- SearchMVPs: combined filters ---

    [Fact]
    public void SearchMVPs_CombinedFilters_Intersects()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice", country: "US", tech: ["Azure"]),
            MakeProfile("2", "Bob", country: "UK", tech: ["GitHub"]),
            MakeProfile("3", "Charlie", country: "US", tech: ["GitHub"]));

        var results = tools.SearchMVPs(namePart: null, awards: null, countries: "US", tech: "GitHub");
        Assert.Single(results);
        Assert.Equal("Charlie", results[0].Name);
    }

    [Fact]
    public void SearchMVPs_CombinedFilters_NoMatch_ReturnsEmpty()
    {
        var tools = CreateTools(
            MakeProfile("1", "Alice", country: "US", tech: ["Azure"]),
            MakeProfile("2", "Bob", country: "UK", tech: ["GitHub"]));

        var results = tools.SearchMVPs(namePart: null, awards: null, countries: "JP", tech: "Rust");
        Assert.Empty(results);
    }

    // --- SearchMVPs: results are MvpProfile list ---

    [Fact]
    public void SearchMVPs_ReturnsFullMvpProfiles()
    {
        var tools = CreateTools(MakeProfile("abc", "Alice", country: "US", tech: ["Azure"]));

        var results = tools.SearchMVPs(namePart: null, awards: null, countries: null, tech: null);
        Assert.Single(results);
        Assert.Equal("Alice", results[0].Name);
        Assert.Equal("US", results[0].Country);
        Assert.Contains("Azure", results[0].TechnologyFocusArea);
    }

    // --- SearchMVPs: returns List<T> not IEnumerable ---

    [Fact]
    public void SearchMVPs_ReturnsListType()
    {
        var tools = CreateTools(MakeProfile("1", "Alice"));
        var results = tools.SearchMVPs(null, null, null, null);
        Assert.IsType<List<MvpProfile>>(results);
    }
}
