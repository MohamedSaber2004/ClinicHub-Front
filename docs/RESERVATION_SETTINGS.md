# إعدادات الحجز في صفحة إعدادات العيادة — الفوائد

> **Scope:** The "إعدادات الحجز" (Booking Configuration) card on the clinic owner's Settings page
> (`Clinic/Settings`, `ClinicController.Settings`).

## What the reservation settings are

From the settings page (matches `prompts/prompt.txt`):

| Field | UI Label | DTO Property | Default |
|-------|----------|--------------|---------|
| رسوم الكشف | رسوم الكشف (جنيه) | `ClinicSettingsDto.ConsultationFee` | 0 |
| العملة | العملة | `ClinicSettingsDto.Currency` | EGP |
| أقصى أيام للحجز المسبق | أقصى أيام للحجز المسبق | `ClinicSettingsDto.MaxAdvanceBookingDays` | 30 |
| مهلة تأكيد الحجز | مهلة تأكيد الحجز (دقائق) | `ClinicSettingsDto.ReservationTtlMinutes` | 10 |
| مهلة الإلغاء | مهلة الإلغاء (دقائق) | `ClinicSettingsDto.CancellationWindowMinutes` | 120 (ساعتان) |

Data flows: `GET /api/v1/admin/clinics/settings` → `ClinicController.Settings` → `ViewBag.Settings` →
saved via `ClinicController.SaveSettings` → `UpdateClinicSettingsRequest`.

---

## 1. رسوم الكشف (Consultation Fee)

**What it does:** The fee a patient pays for a single visit (capped/served per 30-minute appointment).

**Benefit:**
- **One price, one source** — the fee is set once by the clinic owner and applied consistently to every booking, avoiding per-doctor price drift and arguments with patients.
- **Revenue clarity** — the clinic owner knows exactly how much each visit generates, making daily/monthly revenue math trivial (`fee × visits`).
- **Prepares billing** — used as the base for appointment payments (and later invoicing/commission calculation).
- **Guides the patient** — the fee is communicated at booking time so patients know the cost before confirming.

---

## 2. العملة (Currency)

**What it does:** The unit in which the consultation fee (and all clinic pricing) is expressed.

**Benefit:**
- **Stops currency mistakes** — a 300 fee is meaningless without knowing whether it is EGP, SAR, USD… The currency field disambiguates every price shown in the clinic UI and payment flows.
- **Fits multi-region deployment** — the same platform serves clinics in different countries; the owner picks their own currency.
- **Feeds payment/ads pricing math** — subscription, appointment and ad amounts are displayed in the clinic's chosen currency, keeping figures consistent end-to-end.

---

## 3. أقصى أيام للحجز المسبق (Max Advance Booking Days)

**What it does:** The maximum number of days in advance a patient may book a 30-minute slot. Enforced on the online booking page (`Clinic/OnlineBooking`): date input `min=today`, `max=today + MaxAdvanceBookingDays`. The backend rejects dates beyond the window with HTTP 400 `Booking.InvalidDate`.

**Benefit:**
- **Protects the schedule** — prevents patients from booking weeks/months ahead, which old schedules (price changes, doctor leave, closures) would orphan.
- **Keeps capacity real** — short windows mean availability reflects the near-term reality; cancellations are fewer and easier to refill.
- **Reduces no-shows** — a shorter lead time keeps intent high (patients who book far out are more likely to forget).
- **Full control per clinic** — a busy clinic can open 7 days, a clinic with long waiting lists can open 30 or more. It adapts to each clinic's demand without code changes.

---

## 4. مهلة تأكيد الحجز (Reservation TTL — minutes)

**What it does:** How many minutes a fresh booking stays "pending/hold" before auto-confirmation or expiry. `ClinicSettingsDto.ReservationTtlMinutes` (field serialized as `reservationTtlMinutes` in `ClinicService.ClinicSettings`, used in `ClinicController` clinic payloads).

**Benefit:**
- **Prevents squatters** — a slot isn't kept forever by an unconfirmed requester; after the TTL it is released for other patients.
- **Encourages fast action** — patients who want the slot confirm quickly; staff aren't chasing stale pending requests.
- **Fair slot rotation** — combined with the fixed 30-minute slot, the TTL guarantees every slot turns over promptly (no blocked hours from abandoned bookings).
- **Smooth front-desk flow** — staff dashboard pending lists stay short and current, so the team only deals with live requests.

---

## 5. مهلة الإلغاء (Cancellation Window — minutes)

**What it does:** How many minutes after the reservation is created a patient may still cancel it (and receive a refund). Once the window passes, the patient can no longer cancel the booking — the backend blocks the cancel action entirely (no cancel, no refund).

**Enforced in:** `CancelAppointmentCommandHandler` — it loads the clinic's `BookingConfiguration`, compares `DateTime.UtcNow` against `appointment.CreatedAt + CancellationWindowMinutes`, and rejects cancellation with HTTP 400 `Booking.CancellationWindowExpired` after the window has closed.

**Benefit:**
- **Protects clinic revenue & schedule** — a patient who paid can't walk away the day of the visit, so slots stay filled and no-shows/refund-churn drop.
- **Clear, patient-friendly policy** — the patient knows upfront they have (by default) 2 hours to change their mind for a full refund.
- **Configurable per clinic** — like the other reservation settings, each clinic owner chooses their own window from the Settings page.

## How these settings work together with the fixed 30-minute duration

| Setting | Role in the booking lifecycle |
|---------|-------------------------------|
| Fixed 30-minute slot | Defines the **unit of time** for every visit |
| Consultation fee | Defines the **price** of that unit |
| Currency | Defines the **unit of money** |
| Max advance booking days | Limits **how far** patients can claim those units |
| Reservation TTL | Limits **how long** an unconfirmed claim holds a unit |
| Cancellation window | Limits **how long after booking** the patient can still cancel + get a refund |

Together they form the complete "booking policy" of the clinic: *who can book, how far ahead, what it costs, in what currency, and how long a pending hold lasts* — all readable at a glance on the Settings page.

---

## Why this belongs on the Clinic Settings page (not per-doctor)

- **Single owner** — only the clinic owner manages these; doctors manage working hours, not pricing or policy.
- **Predictable & consistent** — every doctor and every booking shares the same fee, currency, window and TTL; no per-doctor settings to reconcile.
- **Airtight sync** — the settings page is the source of truth that the booking page, payments and dashboards all read from; the UI always shows what the backend will enforce.

## Backend note

`ReservationTtlMinutes`, `MaxAdvanceBookingDays`, `ConsultationFee`, `Currency`, `CancellationWindowMinutes` and the fixed `30`-minute duration are enforced by the backend; the settings page only reads/saves the first five. When evaluating a clinic's booking page this contract (fields + defaults + HTTP 400 messages) is what the backend must implement.

> **TTL caveat:** `ReservationTtlMinutes` is stored and returned but the appointment TTL hold/auto-expiry (`Appointment.Reserve`/`ExpireReservation`) is **not** wired up yet in the backend — no code currently puts an appointment into the `Reserved` hold with an `ExpiresAt` deadline. The cancellation window (`CancellationWindowMinutes`) is fully enforced.