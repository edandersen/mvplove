#:package Newtonsoft.Json@13.0.3

using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// Parse -n argument
int? maxCount = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-n" && i + 1 < args.Length && int.TryParse(args[i + 1], out int n))
        maxCount = n;
}

const string BaseApi = "https://mavenapi-prod.azurewebsites.net/api";
const string SearchUrl = $"{BaseApi}/CommunityLeaders/search/";
const int PageSize = 100;
const int MaxConcurrency = 5;

var camelCaseSettings = new JsonSerializerSettings
{
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MVP-Fetcher/1.0)");
http.DefaultRequestHeaders.Add("Origin", "https://mvp.microsoft.com");
http.DefaultRequestHeaders.Add("Referer", "https://mvp.microsoft.com/");

// --- Step 1: Collect profile stubs via search ---
var stubs = new List<CommunityLeaderProfile>();
int pageIndex = 1;
int totalAvailable = 0;

Console.WriteLine("Fetching Microsoft MVP profiles...");

do
{
    int remaining = maxCount.HasValue ? maxCount.Value - stubs.Count : PageSize;
    int pageSize = Math.Min(remaining > 0 ? remaining : PageSize, PageSize);

    var payload = new SearchRequest { PageIndex = pageIndex, PageSize = pageSize };
    var requestJson = JsonConvert.SerializeObject(payload, camelCaseSettings);
    var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
    var response = await http.PostAsync(SearchUrl, content);
    response.EnsureSuccessStatusCode();

    var result = JsonConvert.DeserializeObject<SearchResponse>(await response.Content.ReadAsStringAsync());
    if (result?.CommunityLeaderProfiles is null || result.CommunityLeaderProfiles.Count == 0)
        break;

    if (pageIndex == 1)
    {
        totalAvailable = result.FilteredCount;
        int target = maxCount.HasValue ? Math.Min(maxCount.Value, totalAvailable) : totalAvailable;
        Console.WriteLine($"Total MVPs available: {totalAvailable}. Fetching details for {target}...");
    }

    stubs.AddRange(result.CommunityLeaderProfiles);
    Console.WriteLine($"  Search page {pageIndex}: collected {stubs.Count} stubs...");
    pageIndex++;

} while (!maxCount.HasValue ? stubs.Count < totalAvailable : stubs.Count < maxCount.Value);

// --- Step 2: Enrich each stub with full profile details ---
var allProfiles = new List<MvpProfile>(stubs.Count);
var semaphore = new SemaphoreSlim(MaxConcurrency);
var tasks = stubs.Select(async (stub, idx) =>
{
    await semaphore.WaitAsync();
    try
    {
        var id = stub.UserProfileIdentifier;
        var profile = await FetchProfileDetails(http, id);

        var mvp = new MvpProfile
        {
            Name = BuildName(stub),
            Headline = stub.Headline,
            Country = stub.AddressCountryOrRegionName,
            ProfilePictureUrl = stub.ProfilePictureUrl,
            ProfileUrl = $"https://mvp.microsoft.com/en-US/mvp/profile/{id}",
            Id = id,
            Biography = profile?.Biography,
            Company = profile?.CompanyName,
            CompanyRole = profile?.CompanyRole,
            YearsInProgram = profile?.YearsInProgram,
            AwardCategory = profile?.AwardCategory ?? [],
            TechnologyFocusArea = profile?.TechnologyFocusArea ?? [],
            FunctionalRoles = profile?.FunctionalRoles ?? [],
            Languages = (profile?.Languages ?? []).Select(MapLanguage).ToList(),
            SocialNetworks = (profile?.UserProfileSocialNetwork ?? [])
                .Where(s => !s.IsPrivate)
                .Select(s => new SocialLink { Network = s.SocialNetworkName, Url = s.SocialNetworkImageLink })
                .ToList(),
            Contributions = profile?.Contributions ?? [],
            Events = profile?.Events ?? []
        };

        lock (allProfiles) allProfiles.Add(mvp);
        Console.WriteLine($"  Enriched {allProfiles.Count}/{stubs.Count}: {mvp.Name}");
    }
    finally
    {
        semaphore.Release();
    }
});

await Task.WhenAll(tasks);

// Sort by name for consistent output
allProfiles.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "mvps.json");
string json = JsonConvert.SerializeObject(new { total = allProfiles.Count, mvps = allProfiles }, Formatting.Indented);
await File.WriteAllTextAsync(outputPath, json);

Console.WriteLine($"\nDone! {allProfiles.Count} MVP profiles written to {outputPath}");

// --- Helpers ---

static string BuildName(CommunityLeaderProfile p)
{
    string first = p.ScreenNameLocalized ? p.LocalizedFirstName : p.FirstName;
    string last = p.ScreenNameLocalized ? p.LocalizedLastName : p.LastName;
    return $"{first} {last}".Trim().TrimStart('-').Trim();
}

static string MapLanguage(string lang) =>
    lang.Replace("_LANGUAGE", "").Replace("_", " ").ToLowerInvariant() switch
    {
        "english" => "English",
        "spanish" => "Spanish",
        "french" => "French",
        "german" => "German",
        "portuguese" => "Portuguese",
        "dutch" => "Dutch",
        "arabic" => "Arabic",
        "chinese traditional" => "Chinese (Traditional)",
        "chinese simplified" => "Chinese (Simplified)",
        "japanese" => "Japanese",
        "korean" => "Korean",
        "italian" => "Italian",
        "russian" => "Russian",
        "polish" => "Polish",
        "turkish" => "Turkish",
        "hindi" => "Hindi",
        "swedish" => "Swedish",
        "norwegian" => "Norwegian",
        "danish" => "Danish",
        "finnish" => "Finnish",
        "czech" => "Czech",
        "hungarian" => "Hungarian",
        "romanian" => "Romanian",
        var other => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(other)
    };

static async Task<DetailedProfile?> FetchProfileDetails(HttpClient http, string id)
{
    try
    {
        var profileTask = http.GetStringAsync($"https://mavenapi-prod.azurewebsites.net/api/mvp/UserProfiles/public/{id}");
        var contributionsTask = http.GetStringAsync($"https://mavenapi-prod.azurewebsites.net/api/Contributions/HighImpact/{id}/MVP");
        var eventsTask = http.GetStringAsync($"https://mavenapi-prod.azurewebsites.net/api/Events/HighImpact/{id}/MVP");

        await Task.WhenAll(profileTask, contributionsTask, eventsTask);

        var profileResp = JsonConvert.DeserializeObject<UserProfileResponse>(await profileTask);
        var contribResp = JsonConvert.DeserializeObject<ContributionsResponse>(await contributionsTask);
        var eventsResp = JsonConvert.DeserializeObject<EventsResponse>(await eventsTask);

        var detail = profileResp?.UserProfile;
        if (detail is null) return null;

        detail.Contributions = contribResp?.Contributions
            .Select(c => new ContributionItem { Title = c.Title, Description = c.Description, Date = c.Date, Url = c.Url, Type = c.TypeName })
            .ToList() ?? [];
        detail.Events = eventsResp?.Events
            .Select(e => new EventItem { Title = e.Title, Date = e.Date, Url = e.Url, Location = e.Location })
            .ToList() ?? [];

        return detail;
    }
    catch
    {
        return null;
    }
}

// --- Models ---

class SearchRequest
{
    public string SearchKey { get; set; } = "";
    public string AcademicInstitution { get; set; } = "";
    public List<string> Program { get; set; } = ["MVP"];
    public List<string> CountryRegionList { get; set; } = [];
    public List<string> StateProvinceList { get; set; } = [];
    public List<string> LanguagesList { get; set; } = [];
    public List<string> MilestonesList { get; set; } = [];
    public List<string> AcademicCountryRegionList { get; set; } = [];
    public List<string> TechnologyFocusAreaList { get; set; } = [];
    public List<string> IndustryFocusList { get; set; } = [];
    public List<string> TechnicalExpertiseList { get; set; } = [];
    public List<string> TechnologyFocusAreaGroupList { get; set; } = [];
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

class SearchResponse
{
    public List<CommunityLeaderProfile> CommunityLeaderProfiles { get; set; } = [];
    public int FilteredCount { get; set; }
}

class CommunityLeaderProfile
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string LocalizedFirstName { get; set; } = "";
    public string LocalizedLastName { get; set; } = "";
    public string Headline { get; set; } = "";
    public bool ScreenNameLocalized { get; set; }
    public string AddressCountryOrRegionName { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
    public string UserProfileIdentifier { get; set; } = "";
}

class UserProfileResponse
{
    public DetailedProfile? UserProfile { get; set; }
}

class DetailedProfile
{
    public string? CompanyName { get; set; }
    public string? CompanyRole { get; set; }
    public int? YearsInProgram { get; set; }
    public string? Biography { get; set; }
    public List<string> Languages { get; set; } = [];
    public List<string> AwardCategory { get; set; } = [];
    public List<string> TechnologyFocusArea { get; set; } = [];
    public List<string> FunctionalRoles { get; set; } = [];
    public List<SocialNetwork> UserProfileSocialNetwork { get; set; } = [];
    public List<ContributionItem> Contributions { get; set; } = [];
    public List<EventItem> Events { get; set; } = [];
}

class SocialNetwork
{
    public string SocialNetworkName { get; set; } = "";
    public string SocialNetworkImageLink { get; set; } = "";
    public bool IsPrivate { get; set; }
}

class ContributionsResponse
{
    public List<ContributionRaw> Contributions { get; set; } = [];
}

class ContributionRaw
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public string? Url { get; set; }
    public string? TypeName { get; set; }
}

class EventsResponse
{
    public List<EventRaw> Events { get; set; } = [];
}

class EventRaw
{
    public string Title { get; set; } = "";
    public DateTime? Date { get; set; }
    public string? Url { get; set; }
    public string? Location { get; set; }
}

// --- Output Models ---

class MvpProfile
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
}

class SocialLink
{
    public string Network { get; set; } = "";
    public string Url { get; set; } = "";
}

class ContributionItem
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public string? Url { get; set; }
    public string? Type { get; set; }
}

class EventItem
{
    public string Title { get; set; } = "";
    public DateTime? Date { get; set; }
    public string? Url { get; set; }
    public string? Location { get; set; }
}
