# Dynamic Reservation Slot Duration — Frontend Integration Guide

How the **dynamic slot duration** (مدة الحجز الديناميكية) works end-to-end, and how every frontend
must consume it. **Never hard-code `30` as the source of truth** — the duration is always read from
the API.

---

## 1. The data model

The booking duration belongs to **one availability row**, not to the day, doctor, or clinic:

```
DoctorAvailability (per doctor)
├── row:  Monday 09:00–12:00  slotDurationMinutes: 30   ← morning shift
├── row:  Monday 17:00–21:00  slotDurationMinutes: 45   ← evening shift (different!)
└── row:  Tuesday 09:00–17:00 slotDurationMinutes: 60
```

- A day may have **multiple rows** (shifts), each with its **own** `slotDurationMinutes` (1–480).
- Slot generation slices **each row separately**: Monday morning = 30-min slots, Monday evening =
  45-min slots.
- The value `30` appears **only** as: a prefill for a newly added row, and a fallback for legacy
  rows where the stored value is `0`. After a save, the row's value wins.

---

## 2. Where the duration comes from — one endpoint per consumer

| Consumer | Endpoint | What it returns |
|----------|----------|-----------------|
| **Patient booking page** | `GET /api/v1/clinics/{clinicId}/doctors/{doctorId}/slots?date=YYYY-MM-DD` | **Generated slots** per day, each day-entry carries its `slotDurationMinutes` |
| **Doctor dashboard (schedule mgmt)** | `GET/PUT /api/v1/doctors/availability` (+ `/week`) | The raw weekly schedule rows with their durations (see `doctor-availability-api.md`) |
| **Clinic dashboard (settings)** | `GET /api/v1/admin/clinics/settings` | `slotDurationMinutes` = **typical** (most frequent) duration, **read-only** |
| **Mobile doctor details** | `GET /api/v1/doctors/{id}/details` (mobile) | Per-day availability incl. `slotDurationMinutes` |

> ⚠️ The management schedule endpoint (`/doctors/availability`) is **doctor-dashboard only**.
> Patient-facing pages must use the **slots** endpoint — never the management schedule.

---

## 3. Patient booking flow (dynamic slots)

### 3.1 Fetch

```
GET /api/v1/clinics/{clinicId}/doctors/{doctorId}/slots?date=2026-08-03
```

Response (`data`):

```json
{
  "doctorId": "0a1e2b3c-...",
  "clinicId": "a1b2c3d4-...",
  "requestedDate": "2026-08-03",
  "days": [
    {
      "dayOfWeek": "Monday",
      "workingHours": { "from": "09:00", "to": "12:00" },
      "slotDurationMinutes": 30,
      "slots": [
        { "id": "…", "startTime": "09:00", "endTime": "09:30", "isAvailable": true },
        { "id": "…", "startTime": "09:30", "endTime": "10:00", "isAvailable": false }
      ]
    },
    {
      "dayOfWeek": "Monday",
      "workingHours": { "from": "17:00", "to": "21:00" },
      "slotDurationMinutes": 45,
      "slots": [
        { "id": "…", "startTime": "17:00", "endTime": "17:45", "isAvailable": true },
        { "id": "…", "startTime": "17:45", "endTime": "18:30", "isAvailable": true }
      ]
    }
  ]
}
```

Key points:

- **One `days` entry per availability row** — the same `dayOfWeek` can appear twice (morning +
  evening) with **different** `slotDurationMinutes`. Render them as separate time segments.
- `slotDurationMinutes` on the day-entry is **the** value used to generate that segment's slots —
  use it to label the segment (e.g. "30 دقيقة لكل موعد").
- `isAvailable: false` = the slot overlaps an already-booked (non-cancelled) appointment.
- `slots` are already ordered by `startTime`.
- Without `date` (management only): `GET /api/v1/availability?doctorId=&clinicId=` returns the same
  shape with the week's day-entries and **no booking data**.

### 3.2 Booking-window rule

The same rule enforced at appointment creation is now enforced at slot fetch:

- The clinic's `BookingConfiguration.maxAdvanceBookingDays` limits how far ahead slots can be
  fetched (default 30 days).
- Requesting a date beyond the window → **HTTP 400**, localized message `Booking.InvalidDate`
  ("التاريخ غير صالح" / date invalid).
- If the clinic has no booking configuration, no limit applies.

So the frontend should handle the 400 and show the localized message; it should also clamp its own
date picker to `today + maxAdvanceBookingDays` (the clinic settings endpoint returns
`maxAdvanceBookingDays` for this purpose).

### 3.3 Book an appointment

```
POST /api/v1/appointments   (or /reservations — the booking endpoint of the patient flow)
```

The backend **validates the submitted time against the row's live duration**:

- the appointment must fall inside a row of that weekday,
- its length must equal that row's `slotDurationMinutes`,
- its start must be **aligned to the slot grid** of that row.

→ The patient UI must submit exactly a slot it received from the slots endpoint
(`startTime` + `endTime` as returned). Sending a made-up time (e.g. 09:15 for 30-min slots) fails
with HTTP 400 `AppointmentMessages.DoctorNotAvailableAtThisTime`.

> The same alignment rule is enforced when **rescheduling** (`PUT /appointments/{id}`), so a
> rescheduled appointment also snaps to the row's live grid.

---

## 4. Doctor dashboard (managing the durations)

Full contract in `doctor-availability-api.md`. Summary:

- Load: `GET /api/v1/doctors/availability` → flat rows (`dayOfWeek` 0–6, `startTime`/`endTime`
  `HH:mm:ss`, `slotDurationMinutes`).
- Save the week: `PUT /api/v1/doctors/availability/week` with `days[]` — rows from the server keep
  their `id`; new rows omit it; rows removed from the grid are soft-deleted.
- Validation: `slotDurationMinutes` must be 1–480; `endTime` must be after `startTime`.
- After save, re-render from the response (fresh ids).

**UI rules:**

- Show the duration input **inside each row** — never a single global duration field.
- The "typical duration" badge/stat card = **most frequent** `slotDurationMinutes` across the grid,
  recomputed live on every edit/add/remove.
- Empty schedule → seed editable default rows (Sun–Thu 09:00–17:00, 30 min) with **no ids**; the
  backend ignores empty ids on save.

---

## 5. Clinic dashboard (read-only typical duration)

`GET /api/v1/admin/clinics/settings` returns `slotDurationMinutes` computed live as the **most frequent**
duration across the clinic's doctors' availability rows. It is **read-only** — the clinic never
overrides doctor schedules, and the settings-save endpoint ignores any duration value sent.

Fallback chain: live most-frequent value → settings value → `30`. If the doctors' data fetch fails,
fall back silently (no error banner).

---

## 6. Rules of thumb (checklist)

1. Never render a static `30` anywhere as authoritative — always read from the API.
2. Patient booking: use the **slots** endpoint; submit only returned slot times; handle 400
   booking-window and alignment errors with the localized messages.
3. Doctor dashboard: duration input per row; typical-duration badge recomputed live.
4. Clinic settings: show the read-only typical duration; label it as managed from doctor schedules.
5. When the same weekday has multiple rows, treat each as its own segment with its own duration.
6. All error messages arrive localized (default Arabic via `Accept-Language`).

---

## 7. Backend reference

- Slot generation + per-day duration: `Features/Availability/Queries/GetAvailableSlots/`
- Booking-window rule: same folder, `GetAvailableSlotsQueryValidator`
- Appointment duration/alignment validation: `Features/Appointments/Commands/CreateAppointment/`
  and `UpdateAppointment/` validators
- Typical duration: `Features/Clinics/Queries/GetClinicSettings/GetClinicSettingsQueryHandler.cs`
- Routes: `ApiRoutes.Availability` / `ApiRoutes.Slots` in `ClinicHub.API/Routes/ApiRoutes.cs`
- Domain: `DoctorAvailability` (`SlotDurationMinutes` 1–480, default 30)
