# 🔧 Edit Clinic (Admin) — Location Editing on Google Maps — Update for Backend Review

**Date:** 2026-08-08
**Purpose:** Document the frontend change that lets the **admin (superadmin)** change a clinic's
location on Google Maps in the edit-clinic page, so the **backend team** can confirm the API
contract / payload.

---

## What changed

**Before:** the admin "edit clinic" page (`Views/Admin/ClinicDetails.cshtml`) showed a **read-only map**
(static marker, no interaction) and the save payload contained **no coordinates** — the location
could not be changed.

**After:** the map is now editable and the coordinates are saved with the clinic update.

## Frontend behavior (edit-clinic page)

- Places **search box** ("ابحث عن مكان أو عنوان...") — picking a suggestion pans the map, moves the
  marker and fills the coordinates.
- **Click on the map** or **drag the marker** also updates the coordinates.
- Hidden inputs `clinicDetailLat` / `clinicDetailLng` hold the selected location; a small coords
  display shows the current values.

## Call chain (frontend side)

```
Admin/ClinicDetails (ClinicDetails.cshtml)
   └─ POST /Admin/UpdateClinic/{id} (JSON payload)  → AdminController.UpdateClinic
        └─ PUT {base}/admin/clinics/{id}            ← ClinicService.UpdateClinicAsync
             payload now includes:
               "latitude": 31.040900
               "longitude": 31.378500
```

## Files changed

| File | Change |
|------|--------|
| `Views/Admin/ClinicDetails.cshtml` | Editable map + search box + coords display; `buildPayload()` now sends `latitude` / `longitude` |
| `ClinicHub.Services/RequestModels/UpdateClinicRequest.cs` | Added `double? Latitude` / `double? Longitude` |
| `ClinicHub.Services/Services/Implementations/ClinicService.cs` | `UpdateClinicAsync` forwards `latitude` / `longitude` to the API when present |

## What the backend must provide

- **`PUT /admin/clinics/{id}`** must accept `latitude` (double) and `longitude` (double) in the request
  body and persist them. Currently the frontend only sends them **when the map was used** — if the
  location is untouched, the fields are omitted so existing values stay unchanged.
- The clinic detail response (`GET /admin/clinics/{id}`) must return `lat` / `lng` so the map can
  center on the saved location (frontend falls back to Cairo `31.0409, 31.3785` when missing).

## Checklist for the backend review

- [ ] `PUT /admin/clinics/{id}` accepts + persists `latitude` / `longitude`
- [ ] `GET /admin/clinics/{id}` returns `lat` / `lng` for the saved location
- [ ] Sending an update **without** `latitude` / `longitude` keeps the existing coordinates unchanged

---

## Related (same session, frontend only)

- **`Views/Clinic/Settings.cshtml`** (edit clinic — owner side): added Places search box wired to the
  map; picking a place updates the hidden `settingsLat` / `settingsLng` used by `SaveSettings`
  (payload unchanged — it already sent `latitude` / `longitude`).
- **`Views/Home/ClinicRegister.cshtml`**: added the missing search input (CSS class existed, the input
  was never rendered) + Places autocomplete wiring; sets `Lat` / `Lng` form fields.
- **`Views/Admin/Clinics.cshtml`** (create clinic modal): **bug fix** — the map was initialized while
  the Bootstrap modal was hidden (`display:none` → map rendered at 0×0 and clicks never placed the
  marker). The map is now built lazily on `shown.bs.modal` and resized on every open; search box added.
- All map pages now load the Maps API with `libraries=places` (Places Autocomplete).
