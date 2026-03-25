using System.Text.Json;
using mvcmcpmvp.Models;

namespace mvcmcpmvp.Services;

public class MvpDataService
{
    private readonly List<MvpProfile> _profiles;

    public int TotalCount => _profiles.Count;
    public IReadOnlyList<string> Countries { get; }
    public IReadOnlyList<string> AwardCategories { get; }
    public IReadOnlyList<string> TechFocusAreas { get; }

    public MvpDataService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "getmvps", "mvps.json");
        var json = File.ReadAllText(path);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<MvpData>(json, opts)
            ?? throw new InvalidOperationException("Failed to load mvps.json");
        _profiles = data.Mvps;

        Countries = _profiles
            .Select(p => p.Country).Where(c => !string.IsNullOrEmpty(c))
            .Distinct().OrderBy(c => c).ToList().AsReadOnly();

        AwardCategories = _profiles
            .SelectMany(p => p.AwardCategory)
            .Distinct().OrderBy(a => a).ToList().AsReadOnly();

        TechFocusAreas = _profiles
            .SelectMany(p => p.TechnologyFocusArea)
            .Distinct().OrderBy(t => t).ToList().AsReadOnly();
    }

    public MvpProfile? GetById(string id)
        => _profiles.FirstOrDefault(p => p.Id == id);

    public (List<MvpProfile> Results, int Total) Search(
        string? query,
        List<string> awards,
        List<string> countries,
        List<string> tech,
        string? sort,
        int page,
        int pageSize)
    {
        IEnumerable<MvpProfile> q = _profiles;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.ToLowerInvariant();
            q = q.Where(p =>
                p.Name.ToLowerInvariant().Contains(lower) ||
                p.Headline.ToLowerInvariant().Contains(lower) ||
                (p.Biography ?? "").ToLowerInvariant().Contains(lower) ||
                p.AwardCategory.Any(a => a.ToLowerInvariant().Contains(lower)) ||
                p.TechnologyFocusArea.Any(t => t.ToLowerInvariant().Contains(lower)) ||
                p.Country.ToLowerInvariant().Contains(lower) ||
                (p.Company ?? "").ToLowerInvariant().Contains(lower));
        }

        if (awards.Count > 0)
            q = q.Where(p => p.AwardCategory.Any(a => awards.Contains(a)));

        if (countries.Count > 0)
            q = q.Where(p => countries.Contains(p.Country));

        if (tech.Count > 0)
            q = q.Where(p => p.TechnologyFocusArea.Any(t => tech.Contains(t)));

        q = sort switch
        {
            "years_desc" => q.OrderByDescending(p => p.YearsInProgram ?? 0).ThenBy(p => p.Name),
            "years_asc"  => q.OrderBy(p => p.YearsInProgram ?? 0).ThenBy(p => p.Name),
            "country"    => q.OrderBy(p => p.Country).ThenBy(p => p.Name),
            _            => q.OrderBy(p => p.Name)
        };

        var list = q.ToList();
        var total = list.Count;
        var results = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (results, total);
    }

    public List<FilterOption> GetAwardOptions(List<string>? countries = null, List<string>? tech = null)
    {
        IEnumerable<MvpProfile> q = _profiles;
        if (countries?.Count > 0) q = q.Where(p => countries.Contains(p.Country));
        if (tech?.Count > 0) q = q.Where(p => p.TechnologyFocusArea.Any(t => tech.Contains(t)));
        return q.SelectMany(p => p.AwardCategory)
            .GroupBy(a => a)
            .Select(g => new FilterOption { Value = g.Key, Count = g.Count() })
            .OrderByDescending(f => f.Count).ThenBy(f => f.Value)
            .ToList();
    }

    public List<FilterOption> GetCountryOptions(List<string>? awards = null, List<string>? tech = null)
    {
        IEnumerable<MvpProfile> q = _profiles;
        if (awards?.Count > 0) q = q.Where(p => p.AwardCategory.Any(a => awards.Contains(a)));
        if (tech?.Count > 0) q = q.Where(p => p.TechnologyFocusArea.Any(t => tech.Contains(t)));
        return q.Where(p => !string.IsNullOrEmpty(p.Country))
            .GroupBy(p => p.Country)
            .Select(g => new FilterOption { Value = g.Key, Count = g.Count() })
            .OrderByDescending(f => f.Count).ThenBy(f => f.Value)
            .ToList();
    }

    public List<FilterOption> GetTechOptions(List<string>? awards = null, List<string>? countries = null)
    {
        IEnumerable<MvpProfile> q = _profiles;
        if (awards?.Count > 0) q = q.Where(p => p.AwardCategory.Any(a => awards.Contains(a)));
        if (countries?.Count > 0) q = q.Where(p => countries.Contains(p.Country));
        return q.SelectMany(p => p.TechnologyFocusArea)
            .GroupBy(t => t)
            .Select(g => new FilterOption { Value = g.Key, Count = g.Count() })
            .OrderByDescending(f => f.Count).ThenBy(f => f.Value)
            .ToList();
    }
}
