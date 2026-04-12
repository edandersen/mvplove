namespace mvcmcpmvp.Models;

public class MvpData
{
    public int Total { get; set; }
    public List<MvpProfile> Mvps { get; set; } = [];
}

public class MvpProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Headline { get; set; } = "";
    public string Country { get; set; } = "";
    public string? Company { get; set; }
    public string? CompanyRole { get; set; }
    public int? YearsInProgram { get; set; }
    public List<string> AwardCategory { get; set; } = [];
    public List<string> TechnologyFocusArea { get; set; } = [];
    public List<string> FunctionalRoles { get; set; } = [];
    public List<string> Languages { get; set; } = [];
    public string? Biography { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string ProfileUrl { get; set; } = "";
    public List<SocialLink> SocialNetworks { get; set; } = [];
    public List<ContributionItem> Contributions { get; set; } = [];
    public List<EventItem> Events { get; set; } = [];

    public string Initials => string.Concat(
        Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Where(n => n.Length > 0)
            .Select(n => char.ToUpper(n[0])));

    public static string AwardBadgeClasses(string award)
    {
        var a = award.ToUpperInvariant();
        if (a.Contains("AZURE")) return "bg-blue-100 text-blue-800 border border-blue-200";
        if (a.Contains("M365") || a.Contains("MICROSOFT 365")) return "bg-violet-100 text-violet-800 border border-violet-200";
        if (a.Contains("DEVELOPER")) return "bg-emerald-100 text-emerald-800 border border-emerald-200";
        if (a.Contains("FOUNDRY")) return "bg-orange-100 text-orange-800 border border-orange-200";
        if (a.Contains("DATA") || a.Contains("AI")) return "bg-rose-100 text-rose-800 border border-rose-200";
        if (a.Contains("POWER")) return "bg-yellow-100 text-yellow-800 border border-yellow-200";
        if (a.Contains("WINDOWS")) return "bg-cyan-100 text-cyan-800 border border-cyan-200";
        return "bg-slate-100 text-slate-700 border border-slate-200";
    }

    public static string SocialNetworkClasses(string network)
    {
        var n = network.ToUpperInvariant();
        if (n.Contains("LINKEDIN")) return "bg-blue-600 hover:bg-blue-700 text-white";
        if (n.Contains("TWITTER") || n.Contains("X.COM")) return "bg-slate-800 hover:bg-slate-900 text-white";
        if (n.Contains("GITHUB")) return "bg-gray-800 hover:bg-gray-900 text-white";
        if (n.Contains("YOUTUBE")) return "bg-red-600 hover:bg-red-700 text-white";
        if (n.Contains("FACEBOOK")) return "bg-blue-500 hover:bg-blue-600 text-white";
        if (n.Contains("INSTAGRAM")) return "bg-pink-600 hover:bg-pink-700 text-white";
        return "bg-indigo-600 hover:bg-indigo-700 text-white";
    }

    public static string SocialNetworkIcon(string network)
    {
        var n = network.ToUpperInvariant();
        if (n.Contains("LINKEDIN")) return "in";
        if (n.Contains("TWITTER") || n.Contains("X.COM")) return "𝕏";
        if (n.Contains("GITHUB")) return "gh";
        if (n.Contains("YOUTUBE")) return "yt";
        if (n.Contains("FACEBOOK")) return "fb";
        if (n.Contains("INSTAGRAM")) return "ig";
        return "🔗";
    }
}

public class SocialLink
{
    public string Network { get; set; } = "";
    public string Url { get; set; } = "";
}

public class ContributionItem
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public string? Url { get; set; }
    public string? Type { get; set; }
}

public class EventItem
{
    public string Title { get; set; } = "";
    public DateTime? Date { get; set; }
    public string? Url { get; set; }
    public string? Location { get; set; }
}

public class FilterOption
{
    public string Value { get; set; } = "";
    public int Count { get; set; }
}

public class MvpBrowseViewModel
{
    public string? Query { get; set; }
    public List<string> SelectedAwards { get; set; } = [];
    public List<string> SelectedCountries { get; set; } = [];
    public List<string> SelectedTech { get; set; } = [];
    public string? Sort { get; set; }

    // Filter sidebar options
    public List<FilterOption> AwardOptions { get; set; } = [];
    public List<FilterOption> CountryOptions { get; set; } = [];
    public List<FilterOption> TechOptions { get; set; } = [];

    // Grid results (also used in partial)
    public List<MvpProfile> Results { get; set; } = [];
    public int TotalResults { get; set; }
    public int TotalMvps { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
    public bool HasFilters => !string.IsNullOrWhiteSpace(Query)
        || SelectedAwards.Count > 0 || SelectedCountries.Count > 0 || SelectedTech.Count > 0;
}

public class HomeDashboardViewModel
{
    public List<MvpProfile> LongestTenured { get; set; } = [];
    public List<MvpProfile> MostLanguages { get; set; } = [];
    public List<MvpProfile> NewMvps { get; set; } = [];
    public int TotalCount { get; set; }
}

public class MvpGridViewModel
{
    public List<MvpProfile> Results { get; set; } = [];
    public int TotalResults { get; set; }
    public int TotalMvps { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
    public string? Query { get; set; }
    public List<string> SelectedAwards { get; set; } = [];
    public List<string> SelectedCountries { get; set; } = [];
    public List<string> SelectedTech { get; set; } = [];
    public string? Sort { get; set; }
    public bool HasFilters => !string.IsNullOrWhiteSpace(Query)
        || SelectedAwards.Count > 0 || SelectedCountries.Count > 0 || SelectedTech.Count > 0;
}
