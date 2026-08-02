# Clinic Settings API — Frontend Integration Guide (Backend Contract)

This document is the **final backend contract** for the Clinic Settings page (`/Clinic/Settings`).
It supersedes the draft in `clinic-settings-api.md` (the mock page spec). The backend endpoints
are implemented and the DB migration is applied.

---

## 1. Endpoints

Both endpoints are for the **Clinic Owner** only. The clinic is **not** passed in the URL —
it is resolved automatically from the authenticated user's `CurrentClinicId` claim.

| Method | URL | Auth | Purpose |
|--------|-----|------|---------|
| `GET` | `/api/v1/admin/clinics/settings` | Bearer token, `ClinicOwner` | Pre-fill the whole settings form |
| `PUT` | `/api/v1/admin/clinics/settings` | Bearer token, `ClinicOwner` | Save clinic info + booking configuration |

> Note: the older draft suggested `PUT /api/v1/clinics/{clinicId}` — **do not use it**.
> The owner's clinic ID comes from the token, so no `clinicId` is sent.

Add to `ClinicRoutes` in `DoctoryRoutes.cs`:

```csharp
public string Settings => $"{AdminBaseRoute}/settings";
```

---

## 2. Request body (`PUT`) — camelCase

```json
{
  "name": "عيادة القلب التخصصية",
  "responsibleDoctor": "د. سارة أحمد",
  "description": "عيادة متخصصة في أمراض القلب والأوعية الدموية",
  "phone": "01012345678",
  "managerName": "أحمد محمود",
  "location": "الطابق 3 - غرفة 302",
  "specializationId": "3f2504e0-4f89-41d3-9a0c-0305e82c3301",
  "consultationFee": 200,
  "currency": "EGP",
  "maxAdvanceBookingDays": 30,
  "reservationTtlMinutes": 10,
  "latitude": 31.040900,
  "longitude": 31.378500,
  "isActive": true
}
```

### Required vs optional

| Field | Required | Backend validation |
|-------|----------|--------------------|
| `name` | ✅ | Not empty, max 200 chars |
| `specializationId` | ✅ | Must be a valid specialization (Guid) |
| `consultationFee` | ✅ | Must be > 0 |
| `maxAdvanceBookingDays` | ✅ | Must be > 0 |
| `reservationTtlMinutes` | ✅ | Must be > 0 |
| `currency` | ❌ | Max 3 chars; omitted/empty → `"EGP"` |
| `responsibleDoctor` | ❌ | **Read-only, ignored on save** — always the clinic admin's name (see below) |
| `description` / `managerName` / `location` | ❌ | Max 1000 / 200 / 500 chars |
| `phone` | ❌ | Pattern `^01[0125][0-9]{8}$` (Egyptian mobile), max 11 |
| `latitude` / `longitude` | ❌ | Must be sent **together**; ranges `-90..90` / `-180..180`; 6-decimal precision |
| `isActive` | ❌ | Default `true` |

**`responsibleDoctor` (الطبيب المسؤول):** the responsible doctor is **always the clinic admin**
(the logged-in Clinic Owner) — there is no free-text input. The backend loads it from the clinic
admin account's full name on GET, and ignores any value sent on PUT. The frontend should render it
as a **disabled/read-only field** (e.g. display the logged-in owner's name).

---

## 3. Response body (`GET` / `PUT`) — camelCase

`data` shape is identical for both calls (same DTO):

```json
{
  "success": true,
  "data": {
    "name": "عيادة القلب التخصصية",
    "responsibleDoctor": "د. سارة أحمد",
    "description": "عيادة متخصصة في أمراض القلب والأوعية الدموية",
    "phone": "01012345678",
    "managerName": "أحمد محمود",
    "location": "الطابق 3 - غرفة 302",
    "specializationId": "3f2504e0-4f89-41d3-9a0c-0305e82c3301",
    "specializationName": "Cardiology",
    "specializationNameAr": "القلب والأوعية الدموية",
    "latitude": 31.0409,
    "longitude": 31.3785,
    "isActive": true,
    "consultationFee": 200,
    "currency": "EGP",
    "maxAdvanceBookingDays": 30,
    "reservationTtlMinutes": 10,
    "slotDurationMinutes": 30
  },
  "message": "تم بنجاح",
  "errors": {},
  "statusCode": 200
}
```

**Important:**

- `specializationId` + `specializationName`/`specializationNameAr` — pre-select the specialty dropdown
  by **ID** (`data.specializationId`), display the localized name. Populate the options list from
  `GET /api/v1/specializations/active` (already available via `SpecializationService`). Do **not**
  send the specialty name on save — the backend only accepts the Guid.
- `slotDurationMinutes` is **read-only** — it is the appointment/reservation duration managed by the
  doctor-availability feature (`DoctorAvailability.SlotDurationMinutes`). Display it, never send it.
- `latitude`/`longitude` — feed the map pin on load; defaults `31.0409, 31.3785`.
- If the clinic has no booking configuration yet, GET returns defaults: `consultationFee: 0`,
  `currency: "EGP"`, `maxAdvanceBookingDays: 30`, `reservationTtlMinutes: 10`. PUT creates it
  automatically on first save.

---

## 4. Error handling

Same envelope as the rest of the API. Field-level errors come in `errors` (HTTP 400):

```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": {
    "Name": ["Clinic name is required"],
    "SpecializationId": ["Specialization not found"]
  },
  "statusCode": 400
}
```

| Code | Meaning |
|------|---------|
| 400 | Validation failed (`errors` keyed by field name) |
| 401 / 403 | Not authenticated / not a ClinicOwner (no `CurrentClinicId` claim) |
| 404 | Clinic not found |

User-facing messages are localized; default culture is **Arabic**, controlled by the `Accept-Language`
header (same as the invoices API).

---

## 5. Frontend integration checklist (current gaps to fix)

Based on a review of `Views/Clinic/Settings.cshtml` + `Data/MockClinic`:

| # | Item | Status |
|---|------|--------|
| 1 | Form field ids (`settingsName`, `settingsDoctor`, `settingsDesc`, `settingsPhone`, `settingsManager`, `settingsLocation`, `settingsLat`, `settingsLng`, `settingsActive`) map 1:1 to the payload | ✅ already correct |
| 2 | `settingsSpecialty` must send **`specializationId`** (Guid), not the Arabic name | ❌ fix — replace `MockClinic.Specialty` with `specializationId` + localized name from the GET response |
| 3 | Specialty dropdown options from `GET /api/v1/specializations/active` (id → name), not the hardcoded 5 items | ❌ fix |
| 4 | `settingsDoctor` is **read-only** — pre-fill from GET `responsibleDoctor` (clinic admin's name), do not submit a different value | ❌ fix — render disabled |
| 5 | Add inputs: `consultationFee`, `currency`, `maxAdvanceBookingDays`, `reservationTtlMinutes` (booking section) | ❌ add |
| 6 | Show `slotDurationMinutes` as read-only info (e.g. "مدة الحجز: 30 دقيقة") | ❌ add |
| 7 | Replace `MockClinic` with the real GET response (`IClinicService` pattern: request model + response DTO + route in `DoctoryRoutes`) | ❌ add |
| 8 | Wire `#saveSettingsBtn` to `PUT /api/v1/admin/clinics/settings` and show backend errors (`ApiErrorExtractor`) | ❌ add — currently alert-only |

Recommended service contract (mirrors existing `IClinicService` methods):

```csharp
Task<ApiResponse<ClinicSettingsDto>> GetClinicSettingsAsync();
Task<ApiResponse<ClinicSettingsDto>> UpdateClinicSettingsAsync(UpdateClinicSettingsRequest request);
```

---

## 6. Backend implementation reference

- Route: `ApiRoutes.ClinicManagement.Settings` (`/api/v1/admin/clinics/settings`)
- Controller: `ClinicManagementController` → `GetSettings()` / `UpdateSettings()`
- Application: `Features/Clinics/Commands/UpdateClinicSettings/*`, `Features/Clinics/Queries/GetClinicSettings/*`
- DTO: `ClinicSettingsDto` (`Features/Clinics/DTOs/ClinicManagementDto.cs`)
- DB migration: `20260731092311_addclinicsettingsfields` (adds `ResponsibleDoctor`, `ManagerName` to `Clinics`)
