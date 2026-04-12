# Barry — Project History

## Core Context

- **Project:** mvcmcpmvp — MVP Dashboard
- **Stack:** ASP.NET Core 10 MVC, HTMX, Tailwind CSS (CDN), AG-UI chat agent
- **Data source:** `src/getmvps/mvps.json` → `MvpDataService` singleton at startup
- **Key files:** `src/Controllers/MvpController.cs`, `src/Services/MvpDataService.cs`, `src/Models/MvpModels.cs`, `src/Views/Mvp/`, `src/wwwroot/js/agui-client.js`
- **HTMX pattern:** `MvpController.Grid` returns `Views/Mvp/_Grid.cshtml` partial; HTMX swaps `#mvp-results`
- **AG-UI:** Registered at `/mvpcopilot` in `Program.cs` when `OpenAIApiKey` is configured
- **Config keys:** `OpenAIApiKey`, `OpenAIModelName` (default: `gpt-4o`), `CloudflareOriginSecret`
- **Build:** `dotnet build` from `src/`; `dotnet watch` runs on `http://localhost:5170`
- **Owner:** Ed Andersen

## Recent Updates

📌 Team initialized on 2026-04-12. Barry hired as Full-Stack Dev.

## Learnings

Initial setup complete.
