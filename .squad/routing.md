# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, scope, trade-offs | Gary | New features, breaking changes, "should we…" questions |
| Code review | Gary | PR review, pattern enforcement, quality gates |
| .NET MVC, services, controllers | Barry | MvpDataService, MvpController, Models, Program.cs |
| HTMX partials, Razor views | Barry | _Grid.cshtml, filter/search UI, pagination |
| AG-UI chat agent | Barry | agui-client.js, AgentTools, OpenAI integration |
| Tailwind / CSS / styling | Barry | Layout, components, responsive design |
| Deployment / CI | Barry | GitHub Actions workflow, Azure App Service, Cloudflare middleware |
| Testing, QA, edge cases | Nick | Test plans, regression scenarios, HTMX behavior |
| Issue triage | Gary | Analyze `squad` label issues, assign `squad:{member}` labels |
| Session logging | Scribe | Automatic — never needs routing |
| Work queue / backlog | Ralph | Automatic — monitors GitHub issues and PRs |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
