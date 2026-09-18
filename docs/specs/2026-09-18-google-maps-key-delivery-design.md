# Google Maps Key Delivery — Design Spec

**Date:** 2026-09-18
**Status:** Approved (all 4 sections)
**Scope:** Register-clinic map (and all Maps views) load failure. No Leaflet — Google Maps only.

## 1. Problem

The clinic-registration map rendered blank and place search was dead. Verified root cause:
`GoogleMapsOptions.ApiKey` was empty at runtime — no `GoogleMaps` section in
`appsettings*.json`, no `.env` file, no process environment variable — so the Maps
script URL was built with a blank `key=` and Google rejected the load. The old view
failed silently (blank div, `alert()`-only errors).

Proven facts (live runs, not assumptions):

- The API key itself is genuine — Google Geocoding answered `OK` for it.
- The app code path is correct — with the key visible to the process, the page
  renders HTTP 200 with the key inside the Maps script URL, no empty-key script,
  no error box.
- The key is persisted in the Windows **User** environment (registry read-back
  positive) while live processes still read it as empty (stale host environment).

Key chain (Options pattern, already in code, unchanged by this spec):

```text
GoogleMaps__ApiKey (OS env, __ = section nesting)
  -> Program.cs: Configure<GoogleMapsOptions>(GetSection("GoogleMaps"))
  -> IOptions<GoogleMapsOptions> in Home/Admin/Clinic controllers
  -> ViewBag.GoogleMapsApiKey
  -> script https://maps.googleapis.com/maps/api/js?key=...&libraries=places
```

## 2. Design

### 2.1 Key delivery (done, persists)

- Value lives **only** in the environment (`GoogleMaps__ApiKey`), never in a
  tracked file. User scope is set; Machine scope was skipped (no admin rights —
  revisit only if the app runs under full IIS as another account).
- **Mandatory full restart** of whatever hosts the app (Visual Studio, terminal,
  IIS Express) before any retest — Windows does not retroactively inject new
  User variables into running processes.

### 2.2 Google Cloud key requirements

Browser key with HTTP-referrer restrictions covering `localhost:5046/*`,
`localhost:7044/*`, and the production domain. Enabled on the key's project:
Maps JavaScript API, Places API, Geocoding API. Billing enabled on the project.

### 2.3 Verification protocol

1. Restart host, run app, open the register page.
2. Pass = interactive map renders, search box pans the map, coordinates update.
3. Fail = read the red inline error box, then F12 Console and decode:
   - `MissingKeyMapError` → host process still lacks the variable (restart missed).
   - `InvalidKeyMapError` → wrong/restricted key in Cloud Console.
   - `RefererNotAllowedMapError` → add the domain to key referrers.
   - `ApiNotActivatedMapError` → enable the listed API on the project.

### 2.4 Secret hygiene

- The live key currently sits in git-tracked `prompts/prompt.txt` (modified,
  uncommitted). Restrict its referrers immediately; rotate it if the repo is or
  becomes shared; then remove the value from the file.
- Never print or commit the key value. Diagnostics assert on booleans and
  Google status strings only.

## 3. Non-goals

- No Leaflet/OpenStreetMap fallback (explicitly rejected).
- No changes to `Program.cs`, `*.csproj`, `appsettings*.json`, `launchSettings.json`,
  or `wwwroot/js` (project scope rules). Code hardening such as startup key
  validation is deferred unless the never-touch rule is explicitly overridden.
- Production hosting variables (hosting-panel env var + restart) follow the same
  pattern but are executed by whoever owns the host.

## 4. Acceptance

- Register page shows an interactive Google map with working place search and
  coordinate capture, with zero repo file changes for the key itself.
- Every failure mode surfaces the inline error box plus a console code covered
  by section 2.3 — no silent blank maps.
