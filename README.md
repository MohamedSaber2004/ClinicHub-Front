# ClinicHub Frontend — Recent Changes for Backend Review

> **Purpose:** This document explains the recent frontend changes so the **backend team** can review
> whether any API contract / payload / behavior on the backend side needs attention.
> It covers: reservation settings, the confirm-modal fix, and the removal of the web reservation
> features from the clinic owner dashboard (superseded by the patient mobile app).

---

## Summary of changes

| # | Change | Frontend files | Backend impact |
|---|--------|----------------|----------------|
| 1 | `CancellationWindowMinutes` added end-to-end on the settings page | DTO + request + service + controller + view | New field in settings contract (GET + PUT) |
| 2 | Confirm modal no longer fires its action on cancel/close | `_ConfirmModal.cshtml` | None (UI fix) |
| 3 | Online booking page (حجز أونلاين) removed from clinic owner | view + controller actions + route + sidebar | Web no longer calls booking slots/book endpoints |
| 4 | Smart reservation system (نظام الحجز الذكي) removed | view + controller action + route + sidebar + dashboard card | Web no longer calls accept/reject booking endpoints |
| 5 | Doctor section moved to dedicated clinic-side pages (`/Clinic/Doctor*`) — no more `/Doctor/*` links in the clinic dashboard | `ClinicController`, 4 new clinic views, routes, `_ClinicLayout` | None (reuses doctor endpoints) |

---

## 1. Reservation settings — new `cancellationWindowMinutes` field

The clinic settings page now manages **5** reservation settings (was 4).

### Frontend contract (field names exactly as serialized)

| UI label | JSON property | DTO property | Default |
|----------|---------------|--------------|---------|
| رسوم الكشف (جنيه) | `consultationFee` | `ClinicSettingsDto.ConsultationFee` | 0 |
| العملة | `currency` | `ClinicSettingsDto.Currency` | EGP |
| أقصى أيام للحجز المسبق | `maxAdvanceBookingDays` | `ClinicSettingsDto.MaxAdvanceBookingDays` | 30 |
| مهلة تأكيد الحجز (دقائق) | `reservationTtlMinutes` | `ClinicSettingsDto.ReservationTtlMinutes` | 10 |
| **مهلة الإلغاء (دقائق)** | **`cancellationWindowMinutes`** | **`ClinicSettingsDto.CancellationWindowMinutes`** | **120** |

### Call chain (frontend side)

```
Clinic/Settings (Settings.cshtml)
   └─ GET  /api/v1/admin/clinics/settings          → ClinicController.Settings  → ViewBag.Settings
   └─ POST /Clinic/SaveSettings (JSON payload)     → UpdateClinicSettingsRequest
        └─ PUT  {base}/admin/clinics/settings      ← ClinicService.UpdateClinicSettingsAsync
             payload now includes: "cancellationWindowMinutes"
```

### What the backend must provide

- **GET settings** must return `cancellationWindowMinutes` in the response payload (frontend default is 120 when missing).
- **PUT settings** must accept `cancellationWindowMinutes` in the request body (0 is accepted; page validates `>= 1` client-side).
- The cancellation window (`CancellationWindowMinutes`) is **enforced backend-side** in `CancelAppointmentCommandHandler`
  (HTTP 400 `Booking.CancellationWindowExpired` when the window has passed) — this is unchanged, the frontend only reads/saves it.

### Notes

- `ReservationTtlMinutes` is stored/returned but the appointment TTL hold/auto-expiry is **not wired up yet** in the backend (documented in `docs/RESERVATION_SETTINGS.md`).
- The fixed **30-minute** slot duration is unchanged; the settings page shows it as read-only.

---

## 2. Confirm modal fix (UI only — no backend impact)

**Bug:** Closing the confirm modal (إلغاء button, ESC key, or backdrop click) still executed the action
(approve booking, check-in, delete, …) because the modal invoked the callback with `false` on close
and most call sites ignored the flag.

**Fix (`Views/Shared/_ConfirmModal.cshtml`):** the callback now fires **only** when clicking **تأكيد**.
Cancel / ESC / backdrop close the modal without invoking the callback.

Affected pages (all now behave correctly):
- Staff: Appointments (approve), Queue (check-in / complete), Index
- Clinic: Index
- Doctor: Index, Appointments
- Admin: Ads (×3)

---

## 3. Online booking (حجز أونلاين) removed from the clinic owner web dashboard

**Reason:** patients reserve appointments through the mobile app, so the web booking page is redundant.

### Removed (frontend only)

| What | File |
|------|------|
| Online booking page | `Views/Clinic/OnlineBooking.cshtml` (deleted) |
| `OnlineBooking` action | `ClinicController.cs` (deleted) |
| `GetDoctorSlots` action | `ClinicController.cs` (deleted) |
| `BookSlot` action | `ClinicController.cs` (deleted) |
| Sidebar item حجز أونلاين | `_ClinicLayout.cshtml` (removed) |
| Route helper `OnlineBooking()` | `Routes/ClinicRoutes.cs` (removed) |

### Backend impact

- The web frontend **no longer calls**:
  - `GET {base}/clinics/{clinicId}/doctors/{doctorId}/slots?date=…` (`GetAvailableSlotsAsync`)
  - `POST {base}/clinics/…/appointments` / booking endpoint (`BookAppointmentAsync`)
- The service-layer wrappers (`IClinicDoctorService.GetAvailableSlotsAsync`, `BookAppointmentAsync`,
  `IClinicDashboardService.GetBookingsAsync` / `AcceptBookingAsync` / `RejectBookingAsync`, and the API route
  helpers) are **kept intact** — they are the contract the **mobile app** uses. Backend endpoints must remain.

---

## 4. Smart reservation system (نظام الحجز الذكي) removed from the clinic owner web dashboard

**Reason:** same as above — the mobile app handles reservations; the static web page
(calendar + email reminders mock) is obsolete.

### Removed (frontend only)

| What | File |
|------|------|
| Appointments page (calendar + reminders) | `Views/Clinic/Appointments.cshtml` (deleted) |
| `ClinicController.Appointments()` action | `ClinicController.cs` (deleted) |
| Sidebar item نظام الحجز الذكي | `_ClinicLayout.cshtml` (removed) |
| Route helper `Appointments()` | `Routes/ClinicRoutes.cs` (removed) |
| طلبات الحجز المعلقة card + stat + JS | `Views/Clinic/Index.cshtml` (removed) |
| Pending-bookings loading in `ClinicController.Index()` | `ClinicController.cs` (simplified) |

### Backend impact

- The web frontend **no longer calls** `GET bookings?status=pending`, `POST bookings/accept`, `POST bookings/reject`.
- Same rule as above: service-layer wrappers + API route helpers are kept for the **mobile app** — backend endpoints must remain.

---

## 5. Doctor section in the clinic owner dashboard — separate clinic-side pages

**What changed:** the clinic owner no longer opens the doctor dashboard pages (`/Doctor/*`). The
**لوحة الطبيب** section of the clinic sidebar now routes to **dedicated clinic-side pages**
(`/Clinic/Doctor*`) that render inside the clinic dashboard layout and reuse the same backend endpoints.

### New clinic pages (clinic owner)

| Sidebar item | Route | Action | View |
|--------------|-------|--------|------|
| مواعيد الأطباء | `/Clinic/DoctorAppointments` | `ClinicController.DoctorAppointments` | `Views/Clinic/DoctorAppointments.cshtml` |
| مرضى العيادة | `/Clinic/DoctorPatients` | `ClinicController.DoctorPatients` | `Views/Clinic/DoctorPatients.cshtml` |
| تاريخ المريض | `/Clinic/DoctorPatientHistory/{patientId}` | `ClinicController.DoctorPatientHistory` | `Views/Clinic/DoctorPatientHistory.cshtml` |
| أوقات عمل الأطباء | `/Clinic/DoctorAvailability` | `ClinicController.DoctorAvailability` | `Views/Clinic/DoctorAvailability.cshtml` |

Supporting actions (same payloads as the doctor dashboard, namespaced under `/Clinic`):
- `PUT /Clinic/UpdateDoctorAppointmentStatus?appointmentId=…` (body: `{ status, notes }`)
- `POST /Clinic/SaveDoctorAvailability` (body: `{ days: [{ dayOfWeek, startTime, endTime, slotDurationMinutes, id? }] }`)

The clinic controller now consumes `IDoctorDashboardService` + `IDoctorService` (already registered) —
no new backend endpoints were added by the frontend.

### How it works (frontend only)

- `_ClinicLayout.cshtml` sidebar **لوحة الطبيب** section links to the new `/Clinic/Doctor*` routes
  (no more `/Doctor/*` links in the clinic owner dashboard).
- The original doctor dashboard (`/Doctor/*`) remains unchanged for pure doctors; `Views/Doctor/_ViewStart.cshtml`
  keeps routing them to `_DoctorLayout`.
- `_ClinicLayout.cshtml` still upgrades the sidebar context to the full clinic-owner menu
  (the `DoctorController` mock context would otherwise shrink the menu) — this now only applies to doctor-role visits.

### Backend impact

- The clinic owner pages consume the existing doctor dashboard endpoints
  (`GET /api/v1/doctors/availability`, appointments, patients, patient history, stats) — unchanged.
- **Review question for backend:** `GET /auth/profile` should return the clinic-owner role for
  owner-doctors, and the doctor endpoints must keep serving them (no role regression when the
  caller is a clinic owner acting on the clinic's doctors).

---

## Checklist for the backend review

- [ ] `GET /admin/clinics/settings` returns `cancellationWindowMinutes` (default 120).
- [ ] `PUT /admin/clinics/settings` accepts `cancellationWindowMinutes`.
- [ ] Booking endpoints used by the **mobile app** remain live (slots, book, bookings accept/reject, get pending).
- [ ] `CancelAppointmentCommandHandler` still enforces the cancellation window (HTTP 400 `Booking.CancellationWindowExpired`).
- [ ] `GET /auth/profile` role for owner-doctors is `ClinicOwner` (clinic sidebar layout depends on it).
