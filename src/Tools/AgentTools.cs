    
using System.ComponentModel;
using mvcmcpmvp.Models;
using mvcmcpmvp.Services;

public class AgentTools
{
    private readonly MvpDataService svc;

    public AgentTools(MvpDataService svc)
    {
        this.svc = svc;
    }

    [Description("Searches for MVPs by various criteria including name, awards, countries, and technologies")]
        public List<MvpProfile> SearchMVPs(
            [Description("Part of the MVP's name to search for (optional)")] string? namePart = null,
            [Description("Comma-separated list of award categories to filter by (optional)")] string? awards = null,
            [Description("Comma-separated list of countries to filter by (optional)")] string? countries = null,
            [Description("Comma-separated list of technologies to filter by (optional)")] string? tech = null)
        {
            Console.WriteLine($"Searching MVPs - name: {namePart}, awards: {awards}, countries: {countries}, tech: {tech}");

            var awardsList = string.IsNullOrWhiteSpace(awards)
                ? new List<string>()
                : awards.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var countriesList = string.IsNullOrWhiteSpace(countries)
                ? new List<string>()
                : countries.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var techList = string.IsNullOrWhiteSpace(tech)
                ? new List<string>()
                : tech.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var (results, _) = svc.Search(
                query: namePart ?? string.Empty,
                awards: awardsList,
                countries: countriesList,
                tech: techList,
                sort: null,
                page: 1,
                pageSize: 20);

            return results.ToList();
        }
}