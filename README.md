# MVP Love 🏆

A beautiful Microsoft MVP profile browser built with **ASP.NET Core 10 MVC**, **HTMX**, and **Tailwind CSS**.

View live at [https://www.mvplove.com](https://www.mvplove.com)!

## What is this?

MVP Love lets you browse, search, and filter all [Microsoft Most Valuable Professional (MVP)](https://mvp.microsoft.com) profiles. It's a server-side rendered web app with live interactive filtering powered by HTMX — no JavaScript framework required.

## Features

- **Browse** all MVP profiles loaded from a local JSON dataset
- **Live search** by name, headline, biography, technology, or country
- **Filter** by Award Category, Country, and Technology Focus Area
- **Sort** alphabetically, by years in program, or by country
- **Paginated** grid of MVP cards with photos, award badges, and stats
- **Detail pages** with full biography, social links, contributions, and events
- Fast, interactive filtering via **HTMX** (no full page reloads)
- Pretty **Tailwind CSS** styling — dark gradient header, color-coded badges

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 10 MVC |
| Interactivity | [HTMX](https://htmx.org) 1.9 |
| Styling | [Tailwind CSS](https://tailwindcss.com) CDN |
| Data | In-memory JSON (no database) |
| Language | C# 13 / .NET 10 |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- (Optional) Node.js — only needed to re-fetch MVP data

### Run the app

```bash
dotnet watch
```

The app starts at **http://localhost:5170**.

### Refresh MVP data

The MVP profiles are stored in `getmvps/mvps.json`. To fetch fresh data from the Microsoft MVP API:

```bash
cd getmvps
dotnet run getmvps.cs              # fetch all ~4,000 MVPs
dotnet run getmvps.cs -- -n 100   # fetch first 100
```

The script calls the official `mavenapi-prod.azurewebsites.net` API used by the MVP portal and saves results to `mvps.json`.

## Project Structure

```
mvcmcpmvp/
├── Controllers/
│   ├── HomeController.cs       # Home / error pages
│   └── MvpController.cs        # Browse (Index), Grid (HTMX partial), Detail
├── Models/
│   └── MvpModels.cs            # MvpProfile, view models, badge helpers
├── Services/
│   └── MvpDataService.cs       # Loads JSON, provides search & filter
├── Views/
│   ├── Mvp/
│   │   ├── Index.cshtml        # Browse page with sidebar + grid
│   │   ├── _Grid.cshtml        # HTMX partial: cards + pagination
│   │   └── Detail.cshtml       # Full MVP profile page
│   └── Shared/
│       └── _Layout.cshtml      # Tailwind layout + HTMX script
├── getmvps/
│   ├── getmvps.cs              # .NET 10 file-based data fetcher script
│   └── mvps.json               # MVP profile data (loaded at startup)
└── Program.cs
```

## How HTMX Works Here

The filter sidebar and search bar send lightweight GET requests to `/Mvp/Grid` which returns only the card grid partial — no full page reload. The browser swaps just the `#mvp-results` div:

```
User types in search box
  → HTMX GET /Mvp/Grid?q=azure&award=M365 (after 400ms debounce)
  → Server filters in-memory list, renders _Grid.cshtml partial
  → HTMX swaps #mvp-results with the response
```

## Roadmap

- [ ] **MCP Server** — expose MVP data via Model Context Protocol for AI agents
- [ ] Full MVP dataset (currently loaded from local JSON)
- [ ] Country flag emojis
- [ ] Dark mode
