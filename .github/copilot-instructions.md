# Copilot Instructions

## Build & Run

All `dotnet` commands run from `src/`:

```bash
dotnet watch          # run with hot reload — http://localhost:5170
dotnet build          # build only
dotnet publish mvcmcpmvp.csproj -c Release -o ./publish
```

There are no tests.

### Refresh MVP data

`getmvps/getmvps.cs` is a standalone .NET script (excluded from the main build). Run it from `src/getmvps/`:

```bash
cd src/getmvps
dotnet run getmvps.cs            # fetch all ~4,000 MVPs (10–20 min)
dotnet run getmvps.cs -- -n 100  # fetch first 100
```

This writes `src/getmvps/mvps.json`, which is the app's only data source.

## Architecture

ASP.NET Core 10 MVC app with no database. All MVP data is loaded from `src/getmvps/mvps.json` into memory at startup by `MvpDataService` (registered as a singleton). There are no writes — the service is read-only.

**HTMX partial rendering:** `MvpController.Grid` is a dedicated HTMX endpoint that returns only `Views/Mvp/_Grid.cshtml`. The full browse page (`Index`) server-renders the initial grid by calling the `_Grid` partial directly. Subsequent filter/search/sort changes hit `/Mvp/Grid` via HTMX GET and swap `#mvp-results`. `MvpBrowseViewModel` (full page) and `MvpGridViewModel` (HTMX partial) are intentionally separate — do not merge them.

**AG-UI chat agent:** Registered at `/mvpcopilot` in `Program.cs` only when `OpenAIApiKey` is configured. It uses `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` and the AG-UI protocol (Server-Sent Events). The agent has one tool: `AgentTools.SearchMVPs`. The front-end client is `wwwroot/js/agui-client.js` (plain JS, no framework). The `/Copilot` page checks for the API key and shows a setup message if absent.

**Tailwind CSS** is loaded from CDN — there is no build step for styles.

**Deployment:** Push to `master` triggers the GitHub Actions workflow, which re-fetches MVP data, publishes, and deploys to Azure App Service (`mvp-love`). Cloudflare sits in front of production; the origin is protected by a `CloudflareOriginSecret` header checked in middleware.

## Key Conventions

- **`getmvps/getmvps.cs` is excluded from the main build** via `<Compile Remove="getmvps/**/*.cs" />` in the csproj. Models defined inside it (e.g., `MvpProfile`) are duplicates of `Models/MvpModels.cs` — they're intentionally separate for the standalone script.

- **Badge/icon helpers are static methods on `MvpProfile`** — `AwardBadgeClasses`, `SocialNetworkClasses`, `SocialNetworkIcon`. They use `ToUpperInvariant()` string matching, not enums. Follow this pattern when adding new social networks or award categories.

- **HTMX form state sync:** The filter sidebar lives in `#filter-form`. Hidden inputs `#hidden-q` and `#hidden-sort` mirror the hero search and sort dropdown so that `hx-include="#filter-form"` captures the full state on every HTMX request. Keep these in sync in any new filter controls.

- **Configuration keys:** `OpenAIApiKey`, `OpenAIModelName` (default: `gpt-4o`), `CloudflareOriginSecret`. Set these in Azure App Service → Configuration → App Settings. Leave empty in development to skip Cloudflare and agent checks.

- **Page size** is hardcoded to 24 in both `MvpController.Index` and `MvpController.Grid`. The `MvpBrowseViewModel.TotalPages` and `MvpGridViewModel.TotalPages` computed properties derive from it.
