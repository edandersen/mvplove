# Squad Decisions

## Active Decisions

No decisions recorded yet.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

--- barry-langs-sort.md ---

# Decision: Language Count Display and Sort

**Author:** Barry  
**Date:** 2026

## What Was Added

### Sort options (`MvpDataService.cs`)
Two new sort cases in the `Search` method switch expression:
- `langs_desc` — order by `Languages.Count` descending, then by name
- `langs_asc` — order by `Languages.Count` ascending, then by name

### Internal test constructor (`MvpDataService.cs`)
An `internal MvpDataService(IEnumerable<MvpProfile>)` constructor that accepts pre-loaded profiles, enabling unit tests to inject data without needing a filesystem. Populates `Countries`, `AwardCategories`, and `TechFocusAreas` the same way the production constructor does.

### InternalsVisibleTo (`mvcmcpmvp.csproj`)
Added `AssemblyAttribute` ItemGroup granting `MvcMcpMvpTests` access to internal members.

### Language count chip (`Views/Mvp/_Grid.cshtml`)
Added a language count chip to each MVP tile footer. Shown only when `Languages.Count > 0`. Uses a globe/translation SVG icon and displays "N lang" / "N langs" with correct pluralization. Aligned to the right of the footer row via `ml-auto`.

### Sort dropdown options (`Views/Mvp/Index.cshtml`)
Added two `<option>` entries after the "Country" option:
- `langs_desc` → "Most languages spoken"
- `langs_asc` → "Fewest languages spoken"

Both options honour the existing `Model.Sort` selected-state pattern.

--- nick-test-project.md ---

# Decision: Test Project Setup for mvcmcpmvp

**Author:** Nick (Tester)  
**Date:** 2026-04-12  
**Status:** Implemented

## Framework Choice

**xUnit** — chosen because it is the standard test framework for ASP.NET Core projects and integrates cleanly with `dotnet test`. No Moq or other mocking library was needed since `MvpDataService` is a pure in-memory service once the internal constructor is available.

## Approach

### Project layout
- Test project at `tests/MvcMcpMvpTests/` (net10.0, xUnit 3.x)
- Project reference to `src/mvcmcpmvp.csproj`
- Added to `mvcmcpmvp.sln`

### Test strategy
- **No file I/O:** All tests use the `internal MvpDataService(IEnumerable<MvpProfile>)` constructor (enabled by `InternalsVisibleTo("MvcMcpMvpTests")` in the csproj). No disk reads, no `IWebHostEnvironment`.
- **Helper methods:** `CreateService(params MvpProfile[])` and `MakeProfile(...)` keep test bodies concise and focused on behaviour.
- **Coverage areas:** `TotalCount`, `GetById` (found/not found), `Search` (query matching by name/biography, award/country filters, all sort keys, pagination edge cases), `GetTopPolyglots` (ordering, exclusion of zero-language profiles, count cap).
- **New sort keys explicitly tested:** `langs_desc` and `langs_asc` including tie-breaking by name and zero-language edge case.

## Result
22 tests written, 22 passing, 0 failing.
