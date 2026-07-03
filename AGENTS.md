# ClinicHub Design Rules

## Scope Restriction
This project is **design-only**. All work is strictly limited to:
- CSS files (`wwwroot/css/*.css`)
- CSHTML view files (`Views/**/*.cshtml`)
- Design system files (`wwwroot/css/design-system.css`)

## Never Touch
- Controllers (`Controllers/*.cs`)
- Models (`Models/*.cs`)
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
- `.badge` / `.badge-success` / `.badge-warning` / `.badge-info` / `.badge-danger` for status chips
- `.icon-wrapper` / `.icon-wrapper--primary` / `--blue` / `--amber` / `--green` for icon containers

## No Inline Styles
Never use `style="..."` in CSHTML. Use CSS classes defined in `site.css` or `design-system.css`.
