# Doctor Availability Management API — Frontend Integration Guide

Backend contract for the **doctor availability management** feature used in the **Doctor Dashboard**
(schedule page — إدارة المواعيد/الأوقات المتاحة).

---

## 1. What this feature is

Each doctor manages a **weekly schedule**: one or more availability rows per day of the week.
Each row defines:

- `dayOfWeek` — the day (Sunday → Saturday)
- `startTime` / `endTime` — working window (e.g. `09:00` → `17:00`)
- `slotDurationMinutes` — how long each booking slot lasts (default 30)

A doctor can have **multiple rows on the same day** (e.g. morning `09:00–13:00` + evening `17:00–21:00`).

### How slots are generated (runtime logic)

When a patient opens a doctor's booking page for a specific date:

1. The system takes the doctor's availability row(s) for **that weekday only**.
2. It slices each row's `startTime → endTime` window into consecutive slots of `slotDurationMinutes`.
3. Already-booked slots are marked unavailable.

So the **effective booking duration is per (doctor, day)** — this is why the clinic settings page only
shows a read-only "typical duration" (most frequent `slotDurationMinutes` across the clinic) and never
overrides it.

---

## 2. Auth & scoping

- **Role required:** `Doctor` (Bearer token). The endpoints also require an active subscription
  permission (`ManageAppointments`), same as the rest of the doctor dashboard.
- **Scoping:** the doctor is resolved **from the token** — you **never send `doctorId`** in the URL or
  body. Each doctor can only read/modify their **own** availability.

Base URL: `/api/v1`

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/doctors/availability` | Get the logged-in doctor's full weekly schedule |
| `POST` | `/doctors/availability` | Create one availability row |
| `PUT` | `/doctors/availability/{id}` | Update one availability row |
| `DELETE` | `/doctors/availability/{id}` | Delete one availability row (soft delete) |
| `PUT` | `/doctors/availability/week` | **Replace the whole week in one call** (recommended for the dashboard save button) |

> ⚠️ The older patient-facing endpoint `GET /availability?doctorId=&clinicId=` returns **generated slots**
> for booking, not the schedule — do not use it for the management UI.

---

## 3. Endpoint details

### 3.1 `GET /doctors/availability` — my schedule

Response `data`: flat list sorted by `dayOfWeek` then `startTime`.

```json
{
  "success": true,
  "data": [
    {
      "id": "b1f6c1a0-...",
      "doctorId": "0a1e2b3c-...",
      "dayOfWeek": 2,
      "startTime": "09:00:00",
      "endTime": "17:00:00",
      "slotDurationMinutes": 30,
      "createdAt": "2026-07-31T10:00:00",
      "updatedAt": null
    },
    {
      "id": "c2d7e2b1-...",
      "doctorId": "0a1e2b3c-...",
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

- `dayOfWeek` is the .NET `DayOfWeek` enum: `0=Sunday ... 6=Saturday`.
- `startTime`/`endTime` are `TimeSpan`, serialized as `"HH:mm:ss"`.
- If the doctor has no schedule yet → `data: []`.
- `404` if the logged-in user is not a doctor record.

### 3.2 `POST /doctors/availability` — create one row

Request body:

```json
{
  "dayOfWeek": 2,
  "startTime": "09:00:00",
  "endTime": "17:00:00",
  "slotDurationMinutes": 30
}
```

`slotDurationMinutes` is optional (defaults to `30`). Response `data`: the created row (same shape as above).

### 3.3 `PUT /doctors/availability/{id}` — update one row

Request body (all fields optional — send only what changed):

```json
{
  "dayOfWeek": 3,
  "startTime": "10:00:00",
  "endTime": "16:00:00",
  "slotDurationMinutes": 45
}
```

Response `data`: the updated row.

### 3.4 `DELETE /doctors/availability/{id}` — delete one row

No body. Response `data`: localized success message.

### 3.5 `PUT /doctors/availability/week` — replace the whole week (bulk save)

The recommended pattern for the dashboard: load the week (`GET`), let the user edit a weekly grid,
then send the **complete** desired schedule. The backend:

- creates rows **without** an `id`
- updates rows whose `id` matches an existing one
- **soft-deletes any existing row whose `id` is not in the payload**

Request body:

```json
{
  "days": [
    { "dayOfWeek": 1, "startTime": "09:00:00", "endTime": "14:00:00", "slotDurationMinutes": 30 },
    { "id": "b1f6c1a0-...", "dayOfWeek": 2, "startTime": "09:00:00", "endTime": "17:00:00", "slotDurationMinutes": 30 },
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
| `id` (week payload) | If provided but does not belong to the doctor → `404` |

Error envelope (same as the rest of the API):

```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": {
    "EndTime": ["End time must be after start time"]
  },
  "statusCode": 400
}
```

| Code | Meaning |
|------|---------|
| 400 | Validation failed / doctor not assigned to a clinic |
| 403 | Slot belongs to another doctor |
| 404 | Availability row not found / doctor record not found |

User-facing messages are localized (default **Arabic**, controlled by `Accept-Language`).

---

## 5. Frontend integration steps

1. **On page load:** `GET /doctors/availability` → group `data` by `dayOfWeek` (0–6) and render the
   weekly grid (rows with start/end/duration per day).
2. **Editing:** allow adding/removing rows per day and changing time/duration.
3. **Save:** build `days[]` from the grid — rows loaded from the server keep their `id`; new rows omit
   it — then `PUT /doctors/availability/week`.
4. **Single-row actions (optional UX):** quick-edit uses `PUT /doctors/availability/{id}` (partial body),
   quick-remove uses `DELETE /doctors/availability/{id}`.
5. **After any mutation:** the response already contains the fresh week for `PUT .../week`; for the
   single-row endpoints, refetch `GET` or update the grid locally.

## 6. Backend implementation reference

- Controller: `DoctorDashboardController` (actions `GetMyAvailability`, `CreateMyAvailability`,
  `UpdateMyAvailability`, `DeleteMyAvailability`, `ReplaceWeeklyAvailability`)
- Application: `Features/DoctorDashboard/Availability/` (queries + commands + validators)
- Domain: `DoctorAvailability` entity (`DoctorId`, `ClinicId`, `DayOfWeek`, `StartTime`, `EndTime`, `SlotDurationMinutes`)
- Routes: `ApiRoutes.DoctorDashboard.Availability*` (`/api/v1/doctors/availability...`)
