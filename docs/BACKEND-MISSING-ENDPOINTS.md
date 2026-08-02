# Backend — Missing Endpoints Report (Doctor Availability Management)

> **Audience:** Backend team (Doctory API).
> **Date:** 2026-08-01
> **Status:** ⚠️ Blocking — the Doctor Dashboard "أوقات العمل المتاحة" (Availability) page returns 404 on the deployed environment.

---

## 1. Summary

The ClinicHub front-end (Doctor Dashboard) calls the Doctory API at:

```
https://doctory-icare.runasp.net
```

**Every doctor-dashboard endpoint works except the doctor availability management endpoints.**
The front-end receives HTTP **404** (route not found) for the availability routes, even though
availability records **exist in the database**. The data is there — the API controller/actions for
these routes are **not deployed** on the server.

A route that exists but requires authentication returns **401**; a route that does not exist returns
**404**. The endpoints below return 404, which means they are **not routed** in the deployed build.

---

## 2. Missing endpoints (all return 404 today)

Base URL: `/api/v1` — Role: `Doctor` (Bearer token). Doctor identity is resolved **from the token**
(never send `doctorId` in URL/body).

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/doctors/availability` | Get the logged-in doctor's full weekly schedule |
| `POST` | `/doctors/availability` | Create one availability row |
| `PUT` | `/doctors/availability/{id}` | Update one availability row (partial body) |
| `DELETE` | `/doctors/availability/{id}` | Delete one availability row (soft delete) |
| `PUT` | `/doctors/availability/week` | **Replace the whole week** (used by the dashboard Save button) |

> ⚠️ Do **not** use the older patient-facing `GET /availability?doctorId=&clinicId=` for this feature —
> it returns **generated booking slots**, not the raw weekly schedule. It exists and works, but is not a
> substitute for the management endpoints.

---

## 3. Request / response contracts

### 3.1 `GET /doctors/availability` — my schedule

Response `data`: flat list sorted by `dayOfWeek` then `startTime`.

```json
{
  "success": true,
  "data": [
    {
      "id": "b1f6c1a0-1111-1111-1111-111111111111",
      "doctorId": "0a1e2b3c-2222-2222-2222-222222222222",
      "dayOfWeek": 2,
      "startTime": "09:00:00",
      "endTime": "17:00:00",
      "slotDurationMinutes": 30,
      "createdAt": "2026-07-31T10:00:00",
      "updatedAt": null
    },
    {
      "id": "c2d7e2b1-3333-3333-3333-333333333333",
      "doctorId": "0a1e2b3c-2222-2222-2222-222222222222",
      "dayOfWeek": 2,
      "startTime": "17:00:00",
      "endTime": "21:00:00",
      "slotDurationMinutes": 30,
      "createdAt": "2026-07-31T10:05:00",
      "updatedAt": null
    }
  ],
  "message": "تم بنجاح",
  "errors": {},
  "statusCode": 200
}
```

- `dayOfWeek` = .NET `DayOfWeek`: `0=Sunday ... 6=Saturday`.
- `startTime`/`endTime` = `TimeSpan`, serialized as `"HH:mm:ss"`.
- Multiple rows **on the same day** are allowed (e.g. morning + evening shifts).
- No schedule yet → `data: []`.

### 3.2 `POST /doctors/availability` — create one row

Request:

```json
{ "dayOfWeek": 2, "startTime": "09:00:00", "endTime": "17:00:00", "slotDurationMinutes": 30 }
```

`slotDurationMinutes` optional (default 30). Response `data`: the created row (same shape as GET).

### 3.3 `PUT /doctors/availability/{id}` — update one row

Request (all fields optional — send only what changed):

```json
{ "dayOfWeek": 3, "startTime": "10:00:00", "endTime": "16:00:00", "slotDurationMinutes": 45 }
```

Response `data`: the updated row.

### 3.4 `DELETE /doctors/availability/{id}` — delete one row

No body. Response `data`: localized success message.

### 3.5 `PUT /doctors/availability/week` — replace the whole week (bulk save)

The dashboard loads the week, lets the doctor edit the grid, then sends the **complete** desired
schedule. Backend behavior:

- rows **without** `id` → create
- rows whose `id` matches an existing row → update
- existing rows whose `id` is **not** in the payload → **soft delete**

Request:

```json
{
  "days": [
    { "dayOfWeek": 1, "startTime": "09:00:00", "endTime": "14:00:00", "slotDurationMinutes": 30 },
    { "id": "b1f6c1a0-1111-1111-1111-111111111111", "dayOfWeek": 2, "startTime": "09:00:00", "endTime": "17:00:00", "slotDurationMinutes": 30 },
    { "dayOfWeek": 4, "startTime": "17:00:00", "endTime": "21:00:00", "slotDurationMinutes": 45 }
  ]
}
```

Response `data`: the full updated week (same shape as `GET`).

---

## 4. Validation rules (HTTP 400)

| Field | Rule |
|-------|------|
| `dayOfWeek` | Must be a valid `DayOfWeek` (0–6) |
| `startTime` | Required |
| `endTime` | Required, must be **after** `startTime` |
| `slotDurationMinutes` | `1`–`480` (max 8h), default 30 |
| `id` (week payload) | If provided but not owned by the doctor → `404` |

Error envelope (same as the rest of the API):

```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": { "EndTime": ["End time must be after start time"] },
  "statusCode": 400
}
```

| Code | Meaning |
|------|---------|
| 400 | Validation failed / doctor not assigned to a clinic |
| 403 | Slot belongs to another doctor |
| 404 | Availability row not found / doctor record not found |

User-facing messages are localized (default **Arabic**, controlled by `Accept-Language` header).

---

## 5. Why it matters (impact)

- The Doctor Dashboard page **`/Doctor/Availability`** (أوقات العمل المتاحة) is fully implemented in the
  front-end and consumes exactly these routes.
- Until these endpoints are deployed, doctors **cannot view or edit** their weekly schedule.
- The booking system depends on this data (`slotDurationMinutes` per row is the authoritative booking
  duration used by the patient slots endpoint).

---

## 6. How to verify after deployment

```bash
# Expect 401 when unauthenticated (route exists) — NOT 404
curl -s -o /dev/null -w "%{http_code}\n" https://doctory-icare.runasp.net/api/v1/doctors/availability

# With a doctor Bearer token — expect 200 + weekly rows
curl -s -H "Authorization: Bearer <DOCTOR_TOKEN>" \
     https://doctory-icare.runasp.net/api/v1/doctors/availability
```

Current status (before fix):

```
GET  /api/v1/doctors/availability      -> 404  (should be 401/200)
PUT  /api/v1/doctors/availability/week -> 404  (should be 401/200)
```

---

## 7. Backend implementation reference

This matches the front-end spec in `docs/doctor-availability-api.md`:

- Controller: `DoctorDashboardController` — actions `GetMyAvailability`, `CreateMyAvailability`,
  `UpdateMyAvailability`, `DeleteMyAvailability`, `ReplaceWeeklyAvailability`
- Application: `Features/DoctorDashboard/Availability/` (queries + commands + validators)
- Domain: `DoctorAvailability` entity (`DoctorId`, `ClinicId`, `DayOfWeek`, `StartTime`, `EndTime`,
  `SlotDurationMinutes`)
- Routes: `ApiRoutes.DoctorDashboard.Availability*` → `/api/v1/doctors/availability...`

---

## 8. What already works (for reference — do not break)

Verified deployed and returning 401 (exists, auth-gated) / 200 (public):

- `GET /api/v1/doctors/dashboard/stats`
- `GET /api/v1/doctors/dashboard/recent-appointments?limit=5`
- `GET /api/v1/doctors/appointments` · `GET /api/v1/doctors/patients` · `GET /api/v1/doctors/patients/{id}/history`
- `PUT /api/v1/doctors/appointments/{id}/status` (+ `accept` / `reject` / `complete`)
- `GET /api/v1/doctors/{id}` · `GET /api/v1/doctors/{id}/details`
- `GET /api/v1/admin/dashboard/stats` · `GET /api/v1/staff/dashboard/stats` · `GET /api/v1/admin/clinics/paginated`
- `GET /api/v1/plans` (public, 200) · `GET /api/v1/specializations/active` (public, 200)
