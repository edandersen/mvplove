# Nick — Project History

## Core Context

- **Project:** mvcmcpmvp — MVP Dashboard
- **Stack:** ASP.NET Core 10 MVC, HTMX, Tailwind CSS (CDN), AG-UI chat agent
- **No formal test framework** — test plans documented in Markdown
- **Key behaviors to test:** filter/search/sort/pagination, HTMX partial swaps, AG-UI chat, Cloudflare middleware
- **Owner:** Ed Andersen

## Recent Updates

📌 Team initialized on 2026-04-12. Nick hired as Tester.

## Learnings

Initial setup complete.

### 2026-04-12 — xUnit test project created

- Created `tests/MvcMcpMvpTests/` xUnit project targeting net10.0.
- Added project reference to `src/mvcmcpmvp.csproj` and added to `mvcmcpmvp.sln`.
- Barry had already landed: `internal MvpDataService(IEnumerable<MvpProfile> profiles)` constructor, `langs_desc`/`langs_asc` sort keys, and `InternalsVisibleTo("MvcMcpMvpTests")` in the csproj.
- **22 tests written and all 22 pass** covering: `TotalCount`, `GetById`, `Search` (query/filters/sort/pagination), `GetTopPolyglots`.
- Tests use the internal constructor via `CreateService(params MvpProfile[])` helper — no file I/O required.
- Sort coverage includes: `years_desc`, `years_asc`, `country`, `langs_desc`, `langs_asc` (with tie-breaking and zero-language edge cases).
