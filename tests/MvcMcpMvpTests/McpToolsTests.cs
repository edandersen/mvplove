using System.Text.Json;
using mvcmcpmvp.Models;
using mvcmcpmvp.Services;
using AspNetCoreMcpServer.Tools;

namespace MvcMcpMvpTests;

public class McpToolsTests
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

    // --- Search: empty inputs ---

    [Fact]
    public void Search_EmptyService_ReturnsEmptyJsonArray()
    {
        var svc = CreateService();
        var result = MvpMcpTool.Search(svc, null, null, null, null);
        Assert.Equal("[]", result);
    }

    // --- Search: single profile ---

    [Fact]
    public void Search_SingleProfile_ReturnsValidJson()
    {
        var svc = CreateService(MakeProfile("abc", "Alice"));
        var result = MvpMcpTool.Search(svc, null, null, null, null);

        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.NotNull(parsed);
        Assert.Single(parsed!);
        Assert.Equal("Alice", parsed[0].Name);
    }

    // --- Search: multiple profiles ---

    [Fact]
    public void Search_MultipleProfiles_ReturnsAll()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice"),
            MakeProfile("2", "Bob"),
            MakeProfile("3", "Charlie"));

        var result = MvpMcpTool.Search(svc, null, null, null, null);
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.NotNull(parsed);
        Assert.Equal(3, parsed!.Count);
    }

    // --- Search: namePart filter ---

    [Fact]
    public void Search_NamePart_FiltersByName()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice Smith"),
            MakeProfile("2", "Bob Jones"),
            MakeProfile("3", "Alice Johnson"));

        var result = MvpMcpTool.Search(svc, namePart: "Alice", null, null, null);
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.NotNull(parsed);
        Assert.Equal(2, parsed!.Count);
        Assert.All(parsed, p => Assert.Contains("Alice", p.Name));
    }

    [Fact]
    public void Search_NamePart_CaseInsensitive()
    {
        var svc = CreateService(MakeProfile("1", "Alice"));
        var result = MvpMcpTool.Search(svc, namePart: "alice", null, null, null);
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.Single(parsed!);
    }

    [Fact]
    public void Search_NamePart_NoMatch_ReturnsEmpty()
    {
        var svc = CreateService(MakeProfile("1", "Alice"));
        var result = MvpMcpTool.Search(svc, namePart: "zzz", null, null, null);
        Assert.Equal("[]", result);
    }

    // --- Search: awards filter ---

    [Fact]
    public void Search_Awards_CommaSeparated()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", awards: ["Azure"]),
            MakeProfile("2", "Bob", awards: ["Developer"]),
            MakeProfile("3", "Charlie", awards: ["Azure", "Developer"]));

        var result = MvpMcpTool.Search(svc, null, "Azure,Developer", null, null);
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.Equal(3, parsed!.Count);
    }

    [Fact]
    public void Search_Awards_SingleValue()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", awards: ["Azure"]),
            MakeProfile("2", "Bob", awards: ["Developer"]));

        var result = MvpMcpTool.Search(svc, null, "Azure", null, null);
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.Single(parsed!);
        Assert.Equal("Alice", parsed[0].Name);
    }

    [Fact]
    public void Search_Awards_WhitespaceTrimmed()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", awards: ["Azure"]),
            MakeProfile("2", "Bob", awards: ["Developer"]));

        var result = MvpMcpTool.Search(svc, null, "  Azure , Developer  ", null, null);
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.Equal(2, parsed!.Count);
    }

    [Fact]
    public void Search_Awards_EmptyString_NoFilter()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", awards: ["Azure"]),
            MakeProfile("2", "Bob", awards: ["Developer"]));

        var result = MvpMcpTool.Search(svc, null, "", null, null);
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.Equal(2, parsed!.Count);
    }

    // --- Search: countries filter ---

    [Fact]
    public void Search_Countries_MultiValue()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: "US"),
            MakeProfile("2", "Bob", country: "UK"),
            MakeProfile("3", "Charlie", country: "AU"));

        var result = MvpMcpTool.Search(svc, null, null, "US,AU", null);
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.Equal(2, parsed!.Count);
    }

    [Fact]
    public void Search_Countries_EmptyString_NoFilter()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: "US"),
            MakeProfile("2", "Bob", country: "UK"));

        var result = MvpMcpTool.Search(svc, null, null, "", null);
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.Equal(2, parsed!.Count);
    }

    // --- Search: tech filter ---

    [Fact]
    public void Search_Tech_MultiValue()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", tech: ["Azure"]),
            MakeProfile("2", "Bob", tech: ["GitHub"]),
            MakeProfile("3", "Charlie", tech: ["AI"]));

        var result = MvpMcpTool.Search(svc, null, null, null, "Azure,AI");
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.Equal(2, parsed!.Count);
    }

    [Fact]
    public void Search_Tech_EmptyString_NoFilter()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", tech: ["Azure"]),
            MakeProfile("2", "Bob", tech: ["GitHub"]));

        var result = MvpMcpTool.Search(svc, null, null, null, "");
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.Equal(2, parsed!.Count);
    }

    // --- Search: combined filters ---

    [Fact]
    public void Search_CombinedFilters_Intersects()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: "US", tech: ["Azure"]),
            MakeProfile("2", "Bob", country: "UK", tech: ["GitHub"]),
            MakeProfile("3", "Charlie", country: "US", tech: ["GitHub"]));

        var result = MvpMcpTool.Search(svc, null, null, "US", "GitHub");
        var parsed = JsonSerializer.Deserialize<List<MvpProfile>>(result);
        Assert.Single(parsed!);
        Assert.Equal("Charlie", parsed[0].Name);
    }

    [Fact]
    public void Search_CombinedFilters_NoMatch_ReturnsEmpty()
    {
        var svc = CreateService(
            MakeProfile("1", "Alice", country: "US", tech: ["Azure"]));

        var result = MvpMcpTool.Search(svc, null, null, "JP", "Rust");
        Assert.Equal("[]", result);
    }

    // --- Search: return type ---

    [Fact]
    public void Search_ReturnsJsonString()
    {
        var svc = CreateService(MakeProfile("1", "Alice"));
        var result = MvpMcpTool.Search(svc, null, null, null, null);

        // Must be valid JSON
        var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public void Search_ResultContainsAllProfileFields()
    {
        var svc = CreateService(MakeProfile("xyz", "Alice", country: "UK", tech: ["Azure"]));
        var result = MvpMcpTool.Search(svc, null, null, null, null);
        var doc = JsonDocument.Parse(result);

        Assert.True(doc.RootElement.GetArrayLength() == 1);
        var el = doc.RootElement[0];
        Assert.Equal("Alice", el.GetProperty("Name").GetString());
        Assert.Equal("UK", el.GetProperty("Country").GetString());
    }
}
