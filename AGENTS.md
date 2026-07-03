# ClinicHub (Doctory)

## What this is
Unified clinic management system (Arabic/RTL) — an ASP.NET Core 8 MVC intermediary platform connecting hospital/clinic administrators with clinic owners. Branded as **"Doctory"** internally.

## Tech stack
- .NET 8, ASP.NET Core MVC, Serilog (Console + File + Seq)
- Bootstrap 5 RTL, jQuery, no JS framework
- `.slnx` solution format (new VS XML-based)

## Key architecture
- Route helper classes in `Routes/` used in views avoid magic strings — always use `@AdminRoutes.Pages.Index()` etc.
- Two layouts: `_Layout.cshtml` (public) and `_DashboardLayout.cshtml` (admin/clinic dashboards)
- Two dashboards: `/Admin` (general manager) and `/Clinic` (daily operations)
- No real auth middleware — `AccountController` uses TempData-based dummy flow
- `Routes/Api/` are empty stubs, not wired up
- UI is fully Arabic RTL (`lang="ar" dir="rtl"`)
- Figma design tokens in `prompts/prompt.txt`

## Commands
```powershell
dotnet run --project ClinicHub --launch-profile https
dotnet build ClinicHub\ClinicHub.csproj
```

## Environment
- Dev server at `http://localhost:5046` / `https://localhost:7044`
- Env config via `appsettings.{env}.json` (Live, Production)
- No test project exists
