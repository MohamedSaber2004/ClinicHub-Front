# Plans & Subscriptions — Frontend Integration Guide

> Audience: Frontend team (web dashboard + mobile).
> Backend: ClinicHub API (ASP.NET Core, versioned `api/v1`).
> This document covers all plan-related changes shipped in this session: **4 plan tiers**, **per-plan feature gating (403)**, **doctor/staff creation limits**, and the **new advanced reports endpoint**.

---

## 1. What changed (summary)

| Change | Impact on frontend |
|---|---|
| Subscription plans went from **2 → 4 tiers** (Basic, Standard, Premium, Enterprise) | Plan names, prices and limits are different. Do **not** hardcode plan names — read them from `GET /api/v1/plans` |
| **Priority support** feature removed (it was never implemented) | Remove any UI badge/label for "priority support" |
| Old **"Advanced"** plan was renamed to **"Standard"** | Existing subscriptions now show "Standard" — don't map names 1:1 from older API docs |
| Each plan adds **exactly one feature** over the previous one | You can render a cumulative feature list from the `features` array |
| Doctor/staff creation is now validated against plan limits + subscription state | New `400` errors with specific messages (see §6) |
| Feature-gated endpoints now return `403` for plans that don't include the feature | Handle `403` with an "upgrade" prompt (see §5) |
| New **advanced reports** endpoint (Premium/Enterprise only) | New dashboard section gated by plan |
| **Online payments (Paymob)** | **Unchanged — available to ALL users, NOT plan-gated.** No permission check was added to payment endpoints |

---

## 2. The 4 plan tiers

| Tier | ID (GUID) | EN / AR name | Monthly / Yearly | Max Doctors | Max Staff | Features gained (cumulative) |
|---|---|---|---|---|---|---|
| **Basic** | `A1111111-1111-1111-1111-111111111111` | Basic / أساسية | 500 / 5000 | 2 | 5 | Core: appointments, patient records, basic reports, staff, doctors |
| **Standard** | `A2222222-2222-2222-2222-222222222222` | Standard / قياسية | 1000 / 10000 | 5 | 15 | Basic + **online booking** |
| **Premium** | `A3333333-3333-3333-3333-333333333333` | Premium / ممتازة | 1500 / 15000 | 10 | 30 | Standard + **advanced reports** |
| **Enterprise** | `A4444444-4444-4444-4444-444444444444` | Enterprise / المؤسسات | 2500 / 25000 | Unlimited (`null`) | Unlimited (`null`) | Premium + **marketing tools (ads)** |

> Enterprise sends `maxDoctors: null` / `maxStaff: null` — treat `null` as **unlimited**.

### Feature keys inside `features` (JSON array of strings)

```
appointments        patient_records      basic_reports
staff_management    doctor_management    online_booking
advanced_reports    marketing_tools
```

| Plan | `features` array |
|---|---|
| Basic | `["appointments","patient_records","basic_reports","staff_management","doctor_management"]` |
| Standard | Basic + `"online_booking"` |
| Premium | Standard + `"advanced_reports"` |
| Enterprise | Premium + `"marketing_tools"` |

### Permission names inside `permissions` (array of enum names)

```
ManageAppointments   PatientRecords   BasicReports   ManageStaff
ManageDoctors        OnlineBooking    AdvancedReports  MarketingTools
```

These are the **same features**, expressed as `SubscriptionPermission` enum names. If you display plan capabilities, you can use either field; `features` is the human/semantic list, `permissions` is the machine-enforced list.

---

## 3. Fetching plans (public, no auth)

```
GET /api/v1/plans
```

Returns all **active** plans ordered by `sortOrder` (Basic → Enterprise). Success response:

```json
{
  "success": true,
  "errors": {},
  "data": [
    {
      "id": "A1111111-1111-1111-1111-111111111111",
      "name": "Basic",
      "nameAr": "أساسية",
      "description": "For small clinics starting out. Up to 2 doctors and 5 staff members.",
      "descriptionAr": "للعيادات الصغيرة الجديدة. حتى 2 أطباء و 5 موظفين.",
      "priceMonthly": 500,
      "priceYearly": 5000,
      "maxDoctors": 2,
      "maxStaff": 5,
      "features": "[\"appointments\",\"patient_records\",\"basic_reports\",\"staff_management\",\"doctor_management\"]",
      "isActive": true,
      "sortOrder": 1,
      "permissions": ["ManageAppointments", "PatientRecords", "BasicReports", "ManageStaff", "ManageDoctors"]
    }
  ],
  "message": null,
  "statusCode": 200
}
```

> **Note:** `features` is a **string containing JSON** — you must `JSON.parse(features)` before using it as an array.

Admin endpoints (SuperAdmin only): `GET /api/v1/admin/plans`, `POST /api/v1/admin/plans`, `PUT /api/v1/admin/plans/{id}`, `DELETE /api/v1/admin/plans/{id}` — these accept/return the same shape plus `maxDoctors` / `maxStaff` / `isActive` / `sortOrder`.

---

## 4. Subscriptions (pricing flow)

```
POST /api/v1/subscriptions/initiate-payment   (ClinicOwner) → Paymob checkout
GET  /api/v1/subscriptions/my                (ClinicOwner) → current subscription + plan
POST /api/v1/subscriptions/my/cancel         (ClinicOwner)
POST /api/v1/subscriptions                   (SuperAdmin, manual)
```

- `initiate-payment` body must include a valid `planId` (use the GUIDs from §2).
- `subscriptions/my` returns the clinic's active subscription incl. the nested plan — **use the returned plan object**, never a cached/hardcoded one.

---

## 5. Plan feature gates → handle `403`

Endpoints below now return **`403 Forbidden`** when the caller's clinic subscription plan does not include the feature:

| Endpoint(s) | Required permission |
|---|---|
| `GET /api/v1/doctors/patients`<br>`GET /api/v1/doctors/patients/{patientId}/history` | `PatientRecords` (all plans have it today) |
| `POST/PUT /api/v1/clinics/{clinicId}/booking-config` | `OnlineBooking` (Standard+) |
| `GET /api/v1/clinics/{clinicId}/ads`<br>`GET /api/v1/ads/packages`<br>`POST /api/v1/clinics/{clinicId}/ads/orders` | `MarketingTools` (Enterprise only) |
| `GET /api/v1/admin/clinics/bookings`<br>`POST /api/v1/admin/clinics/bookings/accept`<br>`POST /api/v1/admin/clinics/bookings/reject` | `ManageAppointments` (all plans) |
| `GET /api/v1/admin/clinics/dashboard/stats` | `BasicReports` (all plans) |
| `GET /api/v1/admin/clinics/advanced-report` | `AdvancedReports` (Premium+) |
| `DoctorDashboard` controller (appointments, availability, …) | `ManageAppointments` (all plans) |

403 response body (from the authorization filter — messages are currently **English**, translate client-side):

```json
{
  "success": false,
  "errors": {},
  "data": null,
  "message": "Your current plan does not include this feature. Please upgrade to access it.",
  "statusCode": 403
}
```

The three possible `message` values:

| `message` | Meaning | Frontend action |
|---|---|---|
| `"Your current plan does not include this feature. Please upgrade to access it."` | Plan too low | Show upgrade prompt → navigate to plans/pricing page |
| `"Active subscription required to access this feature."` | No active (or expired) subscription | Show subscribe prompt |
| `"Clinic not found."` | User has no clinic assigned | Show error / contact support |

**Recommended UX:** any 403 whose message contains "does not include this feature" should open the upgrade dialog with the target plan info (you already have all plans from `GET /api/v1/plans`).

---

## 6. Doctor / staff creation — plan limits (`400`)

Creating a doctor (`POST /api/v1/doctors` …) or staff member (`POST /api/v1/clinic-staff` …) now validates the clinic's active plan limits and subscription state.

Validation errors come back as `400 Bad Request` with the standard error envelope:

```json
{
  "success": false,
  "errors": {
    "ClinicId": ["Doctor limit reached for your current plan. Your plan allows up to 2 doctors."]
  },
  "message": "Validation failed",
  "statusCode": 400
}
```

Messages you can expect (localized server-side — Arabic by default, English with `Accept-Language: en`):

| Scenario | Message (EN) | `{0}` placeholder |
|---|---|---|
| Doctor limit reached | `Doctor limit reached for your current plan. Your plan allows up to {0} doctors.` | plan's `maxDoctors` |
| Staff limit reached | `Staff limit reached for your current plan. Your plan allows up to {0} staff members.` | plan's `maxStaff` |
| No active subscription | `No active subscription. Please subscribe to a plan to add doctors and staff.` | — |

**Recommended UX:** when a doctor/staff creation fails with one of these messages, open the upgrade/subscribe dialog. You can pre-fill the message by comparing current staff/doctor count against the plan's `maxDoctors` / `maxStaff` from `subscriptions/my`.

---

## 7. NEW — Advanced reports (Premium / Enterprise)

```
GET /api/v1/admin/clinics/advanced-report?from=2026-01-01&to=2026-12-31
```

Both query params optional (`from`/`to`, `yyyy-MM-dd`). Returns deeper analytics than the basic dashboard stats:

```json
{
  "success": true,
  "errors": {},
  "data": {
    "from": "2026-01-01T00:00:00",
    "to": "2026-12-31T00:00:00",
    "totalAppointments": 120,
    "totalVisits": 98,
    "completionRate": 81.67,
    "totalRevenue": 24500,
    "averageAppointmentValue": 250,
    "revenueByDoctor": [
      { "doctorId": "…guid…", "doctorName": "Dr. Ahmed", "appointmentCount": 60, "revenue": 15000 }
    ],
    "appointmentsByStatus": {
      "Pending": 10, "Confirmed": 12, "Completed": 98, "Cancelled": 5, "Rejected": 2, "NoShow": 3, "Reserved": 0, "Accepted": 0
    },
    "busiestDays": [
      { "date": "2026-03-15T00:00:00", "appointmentCount": 12 }
    ]
  },
  "message": null,
  "statusCode": 200
}
```

Notes:
- `revenueByDoctor` is sorted by revenue (desc).
- `appointmentsByStatus` keys are the `AppointmentStatus` enum names (always the same set; include all in charts).
- `completionRate` is `completed / total * 100`, rounded to 2 decimals.
- Sending `to` earlier than `from` returns `400` with message `'From' date must be before or equal to 'To' date`.
- Accessible only on Premium/Enterprise → handle the `403` from §5.

**Basic vs Advanced:** the basic dashboard stats endpoint (`/dashboard/stats`) gives today/week/month/year counters — keep it on all plans. The advanced report above (date range + breakdowns) is the Premium/Enterprise value-add.

---

## 8. Payments — available to all users (no plan gate)

The Paymob endpoints were **not** gated and remain available to every authenticated user on every plan:

```
POST /api/v1/payments/initiate            POST /api/v1/payments
POST /api/v1/payments/verify              GET  /api/v1/payments/status/{appointmentId}
POST /api/v1/payments/webhook (webhook)
```

Patients and clinics can pay on any plan. No change needed on the frontend for payments.

---

## 9. Localization notes

- Server returns messages in **Arabic by default**; send `Accept-Language: en` (or `ar`) to switch.
- The three `403` filter messages (§5) are hardcoded **English** — keep client-side translations for those keys.
- New/changed message keys: `Subscriptions.NoActiveSubscription` (added), `Subscriptions.DoctorLimitReached`, `Subscriptions.StaffLimitReached`, `Validation.InvalidDateRange` (existing, reused by advanced report).

---

## 10. Frontend verification checklist

- [ ] Plans screen renders **4 plans** from `GET /api/v1/plans` (names/prices/limits/features parsed from `features` JSON string).
- [ ] No "priority support" references remain in the UI.
- [ ] Pricing/upgrade flow sends the correct plan GUID from §2.
- [ ] `subscriptions/my` is the single source of truth for the clinic's current plan.
- [ ] Creating a doctor/staff member when at limit shows the limit message (from `400` errors) and offers an upgrade path.
- [ ] Clinic with no subscription cannot create doctors/staff and sees the "No active subscription" message.
- [ ] Ads section is hidden for non-Enterprise clinics (API returns `403`).
- [ ] Advanced reports section is hidden for Basic/Standard (API returns `403`).
- [ ] Booking-configuration edit UI is hidden for Basic (API returns `403`).
- [ ] Payment flow works for a Basic-plan clinic (no gate).