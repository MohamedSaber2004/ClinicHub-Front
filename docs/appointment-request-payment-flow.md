# Appointment Request → Accept/Reject → Payment Flow (طلبات الحجز والدفع)

> **Audience:** Backend team (Doctory API) + Mobile team.
> **Scope:** Appointment bookings now follow an **approval + payment** lifecycle:
> patient creates a **request** → clinic staff/doctor **accept or reject** → after acceptance the
> patient is **directed to pay** (Paymob) → only after payment the appointment becomes **confirmed**.
> This doc is the single source of truth for the contracts the dashboards (web) and the mobile app
> consume. The web frontend (`ClinicHub`) already renders per this spec — see §5.

---

## 1. Lifecycle & status model

`status` is an integer on every appointment payload.

| Value | Name | Meaning | Set by |
|---|---|---|---|
| `0` | pending (قيد الانتظار) | Patient submitted the request — **not yet approved** | `POST /api/v1/appointments` (patient booking) |
| `6` | accepted / awaiting-payment (بانتظار الدفع) | Staff/doctor **accepted** → payment link sent to patient; **not yet paid** | staff `approve` OR doctor `accept`/`status=6` |
| `1` | confirmed (مؤكد) | **Payment succeeded** (Paymob webhook) — appointment is final | Paymob webhook (server-side, never client) |
| `3` | completed (مكتمل) | Visit done | staff `complete` / doctor `status=3` |
| `4` | reserved (محجوز مؤقتاً) | Slot temp-hold | backend (short hold while booking) |
| `5` | noshow (لم يحضر) | Patient didn't show | staff/doctor |
| `2` | cancelled (ملغي) | Cancelled / rejected (doctor side) | doctor `status=2` or patient cancel |
| `7` | rejected (مرفوض) | Request rejected (staff side) | staff `reject` |

### Hard rules the backend must enforce

- **No auto-confirmation.** A booking always starts at `0` and can never jump to `1` without payment.
- **Accept → payment link.** The accept response **must** return a Paymob redirect URL (see §4.1).
  The patient pays from the mobile app only (dashboards do **not** open the payment page).
- **Paid is final.** Only `1` allows check-in / complete. `6` (accepted, unpaid) cannot be
  checked in, completed, or re-rejected by dashboards — only cancelled by the patient or expired by a
  payment deadline (if implemented).
- **Reject is final.** `2`/`7` cannot be flipped back.
- Do **not** expose appointment payment initiation to the dashboards — the payment record is created
  **inside** the accept command, automatically.

---

## 2. Payment record (type = 0)

Every accepted appointment creates a payment record (`Type = 0` = موعد مريض — same enum as
`Admin/Payments` today):

| Field | Value |
|---|---|
| `payerId` | The patient's user id (from the appointment's `bookedByUserId`) |
| `type` | `0` (appointment) |
| `amount` | Clinic consultation fee — `ClinicSettingsDto.ConsultationFee` (settings page field already exists) |
| `currency` | `EGP` |
| `method` | Paymob (`1`) for online; staff may record `0` (نقدي) manually from `Admin/Payments` as today |
| `status` | `0` pending → `1` paid (webhook) / `3` refunded (refund flow, existing) |

Refunding an appointment payment (superadmin, existing `POST /api/v1/admin/payments/{id}/refund`)
should return the linked appointment to a cancelled state (`2`) — the ads flow already does this
pattern for ads (`3`).

---

## 3. Dashboard contracts (web — implemented)

### 3.1 Staff (reception) — `ClinicHub` already renders per this spec

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/v1/staff/appointments?status=&date=&patientName=&pageNumber=&pageSize=` | List (server returns `statusLabel`/`statusClass` or raw int — frontend normalizes) |
| `PUT` | `/api/v1/staff/appointments/{id}/approve` | **Accept** → status `6` + create payment + return link (§4.1) |
| `PUT` | `/api/v1/staff/appointments/{id}/reject` | Reject (body `{ reason }`) → status `7` |
| `PUT` | `/api/v1/staff/appointments/{id}/check-in` | Only allowed when status = `1` |
| `PUT` | `/api/v1/staff/appointments/{id}/complete` | → status `3` |

UI expectations (already live in `Views/Staff/Appointments.cshtml`):

- Status filter: قيد الانتظار / **بانتظار الدفع** / مؤكد / ملغي / منتهي.
- `pending` → قبول + رفض buttons (قبول shows a confirm: "سيتم إرسال رابط الدفع للمريض").
- `accepted` → badge **بانتظار الدفع** (amber), no actions, hint "بانتظار دفع المريض".
- `confirmed` → تسجيل وصول button only.
- Reject prompts for an optional reason.

### 3.2 Doctor — already live per this spec

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/v1/doctors/appointments?status=&searchTerm=&startDate=&endDate=&pageNumber=&pageSize=` | List |
| `PUT` | `/api/v1/doctors/appointments/{id}/status` | Body `{ status, notes }` — doctor sends `6` (قبول), `2` (رفض), `3` (إكمال), `5` (لم يحضر) |
| `PUT` | `/api/v1/doctors/appointments/{id}/accept` | Dedicated accept (must behave exactly like `status=6`) |
| `PUT` | `/api/v1/doctors/appointments/{id}/reject` | Dedicated reject (body `{ reason }`) |

UI expectations (already live in `Views/Doctor/Appointments.cshtml` + `Views/Doctor/Index.cshtml`):

- `0` → قبول / رفض buttons (قبول confirms and sends **status `6`**, NOT `1`).
- `6` → badge **بانتظار الدفع** (amber), hint "بانتظار دفع المريض", no actions.
- `1` → إكمال button only.
- `2/7` → no actions.

### 3.3 API response the frontend expects from accept (⚠️ new backend requirement)

Both `approve` (staff) and `accept`/`status=6` (doctor) currently return `data: true`. The new
contract: `data` becomes the created payment envelope so mobile can be notified server-side:

```json
{
  "success": true,
  "data": {
    "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": 6,
    "paymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "amount": 300.00,
    "currency": "EGP",
    "paymobRedirectUrl": "https://accept.paymob.com/...",
    "paymobPaymentKey": "paymob-payment-key"
  },
  "message": "تم قبول الحجز وتم إرسال رابط الدفع للمريض",
  "statusCode": 200
}
```

`paymobRedirectUrl` is also pushed to the patient via **push notification** (mobile §4.1) — the
dashboards don't display it.

---

## 4. Mobile app (patient) contract

### 4.1 After acceptance

1. Patient books a slot (existing `GET /api/v1/clinics/{clinicId}/doctors/{doctorId}/slots?date=` +
   `POST /api/v1/appointments` — unchanged) → request `status 0`.
2. Staff/doctor accepts → backend creates the payment + Paymob checkout and sends a **push
   notification** to the patient:
   - Title: "تم قبول حجزك" — Body: "أكمل الدفع لتأكيد موعدك" — Deep link: pay screen.
3. Patient opens "My appointments" → the appointment shows status **بانتظار الدفع** with a
   **"ادفع الآن"** button → opens `paymobRedirectUrl` (Paymob hosted page).
4. Paymob webhook confirms → status flips `6 → 1` (مؤكد) automatically.
5. If payment fails/expires, the request stays `6` — patient can retry (same link or a fresh one).

### 4.2 "My appointments" endpoint (patient) — ⚠️ new backend requirement

`GET /api/v1/appointments/my?status=` returns the patient's requests with payment info:

```json
{
  "success": true,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "clinicId": "11111111-1111-1111-1111-111111111111",
      "clinicName": "مجمع عيادات السلام الطبي",
      "doctorId": "22222222-2222-2222-2222-222222222222",
      "doctorName": "د. أحمد محمد",
      "date": "2026-08-05",
      "startTime": "10:00",
      "endTime": "10:30",
      "status": 6,
      "rejectionReason": null,
      "payment": {
        "paymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "amount": 300.00,
        "currency": "EGP",
        "paymentStatus": 0,
        "paymobRedirectUrl": "https://accept.paymob.com/..."
      }
    }
  ]
}
```

`paymobRedirectUrl` is present only while `status = 6` (unpaid). Payment status: `0` معلق / `1`
ناجح / `2` فاشل / `3` مسترد.

### 4.3 Patient status labels (mobile UI)

| status | Label | Action shown |
|---|---|---|
| `0` | قيد الانتظار | none (إلغاء optional) |
| `6` | بانتظار الدفع | **ادفع الآن** |
| `1` | مؤكد | details |
| `3` | مكتمل | — |
| `2`/`7` | ملغي / مرفوض | show `rejectionReason` |

---

## 5. What the web frontend already does (already deployed)

- `Views/Staff/Appointments.cshtml` — approve confirm ("سيتم إرسال رابط الدفع للمريض"), reject
  reason prompt, awaiting-payment state (no check-in until confirmed), status filter includes
  بانتظار الدفع.
- `Views/Doctor/Appointments.cshtml` + `Views/Doctor/Index.cshtml` — قبول now calls
  `PUT /doctors/appointments/{id}/status` with **`6`** (not `1`), إكمال only for `1`, badge
  بانتظار الدفع (amber).
- `Views/Doctor/PatientHistory.cshtml` — same labels.
- `StaffDashboardService` (`ClinicHub.Services`) — `accepted`/`awaiting-payment` both map to
  "بانتظار الدفع".
- Patient web booking (`Views/Clinic/OnlineBooking.cshtml`) — success message now says
  "تم إرسال طلب الحجز بنجاح، وسيتم إعلامك بعد موافقة العيادة".

### Web frontend does NOT do (by design)

- Dashboards never open the Paymob page for appointments (mobile-only payment).
- No client-side status computation — the API `status` is the source of truth.

---

## 6. Error codes summary

| Code | When | Typical message (ar) |
|---|---|---|
| `400` | Accept on non-`0` appointment / reject on non-`0` / no clinic fee configured | `لا يمكن قبول هذا الحجز في حالته الحالية` / `لا توجد رسوم استشارة محددة للعيادة` |
| `401` | Missing/invalid token | — |
| `403` | Appointment belongs to another clinic/doctor | `لا تملك صلاحية التعامل مع هذا الموعد` |
| `404` | Appointment not found | `الموعد غير موجود` |
| `409` | Payment already exists for the appointment (double accept) | `تم قبول هذا الحجز مسبقاً` |

All errors use the standard `ApiResponse<T>` envelope: `{ success: false, message, errors, statusCode }`,
localized (default Arabic, `Accept-Language` header).

---

## 7. Backend implementation checklist

- [ ] Accept commands (staff `approve`, doctor `accept` / `status=6`) → set status `6` + create
      payment `type=0` + initiate Paymob → return `{ appointmentId, paymentId, amount, paymobRedirectUrl }`.
- [ ] Paymob webhook success → appointment `6 → 1` (idempotent).
- [ ] Push notification to patient on accept (with payment link).
- [ ] `GET /api/v1/appointments/my` (patient) with payment payload.
- [ ] Refund of `type=0` payment → appointment back to `2`.
- [ ] Guard: check-in/complete reject `status != 1`; accept/reject only from `0`.
- [ ] Clinic fee: fall back gracefully when `ConsultationFee` is not set.
