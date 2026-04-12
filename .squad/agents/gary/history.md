# Gary — Project History

## Core Context

- **Project:** mvcmcpmvp — MVP Dashboard
- **Stack:** ASP.NET Core 10 MVC, HTMX, Tailwind CSS (CDN), AG-UI chat agent
- **Data:** All MVP data loaded from `src/getmvps/mvps.json` at startup by `MvpDataService` (singleton, read-only)
- **Key patterns:** MvpController.Grid for HTMX partials; MvpBrowseViewModel vs MvpGridViewModel kept separate; AG-UI at `/mvpcopilot`
- **Deployment:** Azure App Service (`mvp-love`), Cloudflare in front, GitHub Actions on `master`
- **Owner:** Ed Andersen

## Recent Updates

📌 Team initialized on 2026-04-12. Gary hired as Lead.

## Learnings

Initial setup complete.
