# ClinicHub Design Rules

## Scope Restriction
This project is **design-only**. All work is strictly limited to:
- CSS files (`wwwroot/css/*.css`)
- CSHTML view files (`Views/**/*.cshtml`)
- Design system files (`wwwroot/css/design-system.css`)
- Data files (`Data/*.cs`) — mock data only, no business logic

## Controller Usage (Limited)
Controllers may be touched ONLY for:
- Passing data to views via `ViewBag` / `ViewData`
- **NO business logic** in controllers — routing and data passing only

## Never Touch
- Models (`Models/*.cs`) — unless creating simple DTOs for mock data
- Backend logic (`Program.cs`, `*.csproj`)
- Configuration files (`appsettings*.json`, `launchSettings.json`)
- JavaScript files (`wwwroot/js/*.js`)
- Library files (`wwwroot/lib/**`)

## Design System Reference
Use tokens from `wwwroot/css/design-system.css`:
- `var(--clr-*)` for colors
- `var(--space-*)` for spacing
- `var(--fs-*)` / `var(--fw-*)` for typography
- `var(--radius-*)` for border radius
- `.badge` / `.badge-success` / `.badge-warning` / `.badge-info` / `.badge-danger` / `.badge-primary` for status chips
- `.icon-wrapper` / `.icon-wrapper--primary` / `--blue` / `--amber` / `--green` for icon containers

## UI Simplicity Rule
All UI designs must avoid complex business logic. Interactive elements must be straightforward to implement.

## No Inline Styles
Never use `style="..."` in CSHTML. Use CSS classes defined in `site.css` or `design-system.css`.

## Project Skills (`.opencode/skills/`)
- **execution** — Code implementation: building views, consuming API endpoints, controller actions, design-system conventions. Read before writing any code.
- **planning** — Task breakdown before starting large features. Use when a plan is needed first.
- **sync-execution-skill** — Updates the execution skill after new implementations are added to the project.

## Default Agent
This project uses `clinichub-dev` agent (defined in `.opencode/agents/clinichub-dev.md`). It has access to all project skills above.
