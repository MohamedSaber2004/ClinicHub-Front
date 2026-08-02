# Superadmin Payments API — Endpoints Guide

This document defines the **backend API endpoints** needed to power the Superadmin
**Payments page** (`Admin/Payments` — المدفوعات والمعاملات المالية) in the web dashboard.
The page currently renders mock data (`MockData.GetPayments()` / `GetPaymentStats()` /
`GetPaymentDetail(id)`) and must be wired to real endpoints.

---

## 1. Overview

The Superadmin payments page lists **every money transaction that happens on the platform**
with filters, KPI stats, a detail view, manual payment registration, and refunds.
There are **three payment tasks** (types) the page must cover:

| # | Payment type | Arabic label (UI) | Who pays | Why |
|---|--------------|-------------------|----------|-----|
| 1 | **Appointments** | موعد مريض | Patient | Consultation fee (كشف) paid at booking / visit |
| 2 | **Clinic subscriptions** | اشتراك عيادة | Clinic | Recurring plan payment (monthly / yearly) |
| 3 | **Ads services** | خدمة إعلانية | Clinic | Paid promotion — **only clinics on the Advanced plan can purchase** |

---

## 2. Common conventions

- **Base URL:** `/api/v1`
- **Auth:** `Authorization: Bearer <token>` — every endpoint requires the `SuperAdmin` role
  (`[RoleAuthorize(SuperAdmin)]`); also requires an active platform/admin account.
- **Language:** `Accept-Language: ar` (default) — all user-facing messages are localized (Arabic).
- **Response wrapper:** `ApiResponse<T>` → `{ success, data, message, errors, statusCode }`.
- **Pagination:** `PagginatedResult<T>` → `{ items, totalCount, totalPages, pageNumber, pageSize, hasPreviousPage, hasNextPage }`.

### Enums (payment type / status / method)

```csharp
public enum PaymentType   // sent by the backend in every payment DTO
{
    Appointment = 0,      // موعد مريض
    Subscription = 1,     // اشتراك عيادة
    Ads = 2               // خدمة إعلانية
}

public enum PaymentStatus
{
    Pending  = 0,         // معلق
    Success  = 1,         // ناجح
    Failed   = 2,         // فاشل
    Refunded = 3          // مسترد
}

public enum PaymentMethod
{
    Cash      = 0,        // نقدي
    BankTransfer = 1,     // تحويل بنكي
    PaymobCard   = 2,     // Paymob - بطاقة ائتمان
    PaymobWallet = 3      // Paymob - محفظة إلكترونية
}
```

---

## 3. Payment type 1 — Appointments (موعد مريض)

**Source of the payment:** a patient pays the consultation fee (كشف) when booking an
appointment (online via Paymob) or when checking in at the clinic (cash / card).
The backend must record a payment settlement with `type = Appointment` linked to the
appointment when the payment is confirmed.

### Needed endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/v1/admin/payments?type=0` | List of appointment payments (filtered by `type=0`) |
| `GET` | `/api/v1/admin/payments/{id}` | Payment detail (payer, item name, timeline) |
| `GET` | `/api/v1/admin/payments/stats?type=0` | Appointment-payment KPI stats |

**List filters that must work with `type=0`:** `PageNumber`, `PageSize`, `SearchTerm`
(payer name / reference number), `Status`, `Method`, `FromDate`, `ToDate`.

**Detail fields expected by the UI:** payer (name / type / email / phone), item name
(e.g. "كشف موعد"), amount, method, transaction id, reference number (e.g. `PM-2026-003`),
status, date, timeline entries (created → paid → confirmed), notes.

---

## 4. Payment type 2 — Clinic subscriptions (اشتراك عيادة)

**Source of the payment:** a clinic pays a plan subscription (monthly / yearly) through
`Clinic/Subscribe` → Paymob checkout (`InitiatePaymentAsync`). The backend must persist the
payment record with `type = Subscription` and link it to the subscription (plan id, period).

### Needed endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/v1/admin/payments?type=1` | List of subscription payments |
| `GET` | `/api/v1/admin/payments/{id}` | Detail (e.g. "اشتراك سنوي - 2026") |
| `GET` | `/api/v1/admin/payments/stats?type=1` | Subscription-revenue KPI stats |
| `POST` | `/api/v1/admin/payments/manual` | Record a manual payment (bank transfer confirmation) |

**Detail fields expected:** clinic name / email / phone as payer, item name = plan name +
period (e.g. "اشتراك سنوي - 2026"), amount, method (Paymob / bank transfer), transaction id,
reference number, status, timeline (created → payment initiated → success/failure), notes.

---

## 5. Payment type 3 — Ads services (خدمة إعلانية)

**Eligibility rule (important):** only clinics subscribed to the **Advanced plan** may
purchase ads services (e.g. homepage banners, featured clinic placement).
The backend must **reject ads purchases** for clinics whose active plan is not Advanced.

### Needed endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/v1/admin/ads/eligible-clinics` | Clinics that can buy ads (active subscription + Advanced plan only) — fills the payer dropdown in the manual-payment modal |
| `POST` | `/api/v1/admin/ads/orders` | Create an ads order (clinicId, adPackageId, duration) → returns a Paymob checkout URL |
| `GET` | `/api/v1/admin/payments?type=2` | List of ads payments |
| `GET` | `/api/v1/admin/payments/{id}` | Detail (e.g. "بانر إعلاني - شهرين") |
| `POST` | `/api/v1/admin/payments/{id}/refund` | Refund an ads payment (the UI shows refunds for ads) |

**Eligibility check logic (backend):**

```
clinic.HasActiveSubscription
  AND clinic.ActivePlan.IsAdvanced   // e.g. plan feature "advanced_reports" / plan tier flag
→ eligible = true
→ else 403: "لا يمكن شراء الخدمات الإعلانية إلا للباقات المتقدمة"
```

**Ads order request example:**

```json
{
  "clinicId": "00000001-0000-0000-0000-000000000001",
  "adPackageId": "guid",
  "durationDays": 60,
  "returnUrl": "https://dashboard.doctory.com/Home/PaymentResult"
}
```

---

## 6. Shared endpoints (whole page)

### 6.1 List — `GET /api/v1/admin/payments`

Powers the payments table (with type / status / method filters + search + pagination).

**Query parameters:** `PageNumber` (1), `PageSize` (20), `Type` (0/1/2, empty = all),
`Status` (0-3), `Method` (0-3), `FromDate`, `ToDate` (YYYY-MM-DD), `SearchTerm` (payer, code, ref).

**Response `data` item shape:**

```json
{
  "id": "guid",
  "code": "#P-01",
  "type": 1,
  "payer": "مجمع عيادات السلام الطبي",
  "amount": 5000.00,
  "currency": "EGP",
  "method": 2,
  "status": 1,
  "date": "2026-07-01T10:30:00Z",
  "refNumber": "PM-2026-001"
}
```

### 6.2 Detail — `GET /api/v1/admin/payments/{id}`

```json
{
  "id": "guid",
  "code": "#P-03",
  "type": 0,
  "payer": "محمد عمر (مريض)",
  "payerType": "Patient",
  "payerEmail": "mohamed@email.com",
  "payerPhone": "01111122222",
  "itemName": "كشف موعد",
  "amount": 200.00,
  "method": 3,
  "transactionId": "a1b2c3d4e5f6",
  "refNumber": "PM-2026-003",
  "status": 1,
  "date": "2026-07-03T10:30:00Z",
  "notes": "",
  "timeline": [
    { "date": "2026-07-03T10:32:00Z", "text": "تم تأكيد الدفع واستلام المبلغ", "marker": "success" },
    { "date": "2026-07-03T10:30:00Z", "text": "تم إنشاء المعاملة", "marker": "info" }
  ]
}
```

### 6.3 Stats — `GET /api/v1/admin/payments/stats`

Powers the 4 KPI cards at the top of the page. Suggested response `data`:

```json
{
  "todayRevenue": 12400,
  "subscriptionsRevenue": 5000,
  "appointmentsRevenue": 2400,
  "adsRevenue": 5000,
  "pendingCount": 3,
  "successCount": 120,
  "failedCount": 5,
  "refundedCount": 2
}
```

### 6.4 Manual payment — `POST /api/v1/admin/payments/manual`

Registers a manual payment (bank transfer confirmation, cash, offline cheque)
from the "تسجيل دفعة يدوية" modal.

```json
{
  "payerId": "guid",          // clinic / doctor / patient id
  "type": 1,                  // 0=Appointment, 1=Subscription, 2=Ads
  "amount": 5000,
  "method": 1,                // BankTransfer / Cash
  "refNumber": "TRF-2026-8821",
  "notes": "تحويل من البنك الأهلي"
}
```

**Success** → `201` + creates the payment record (status = `Success`).
**Eligibility guard:** when `type = 2` (Ads), validate the payer clinic is on the
Advanced plan, else `403`.

### 6.5 Refund — `POST /api/v1/admin/payments/{id}/refund`

```json
{ "reason": "تم استرداد المبلغ بناءً على طلب العيادة" }
```

Sets status → `Refunded`, adds a timeline entry, returns `200`.

---

## 7. UI mapping (frontend expectations)

| UI element | Backend field |
|------------|---------------|
| Table columns: `#` / الدافع / النوع / المبلغ / طريقة الدفع / الحالة / التاريخ | `code`, `payer`, `type`, `amount`, `method`, `status`, `date` |
| Type badge — اشتراك عيادة / موعد مريض / خدمة إعلانية | `type` → 1 / 0 / 2 |
| Status badge — ناجح / معلق / فاشل / مسترد | `status` → 1 / 0 / 2 / 3 |
| Method text — Paymob بطاقة / Paymob محفظة / تحويل بنكي / نقدي | `method` → 2 / 3 / 1 / 0 |
| Filter selects (type, method, status) | `Type`, `Method`, `Status` query params |
| Detail page — payer card, item name, timeline | `GET /admin/payments/{id}` |
| Manual payment modal — payer dropdown | payers list (subscriptions: clinics; ads: `eligible-clinics` only) |
| KPI stat cards | `GET /admin/payments/stats` |

---

## 8. Error codes

| Code | Meaning |
|------|---------|
| `401` | Missing / invalid / expired admin token |
| `403` | Not SuperAdmin, or ads purchase by a non-Advanced-plan clinic |
| `404` | Payment / clinic / ads order not found |
| `400` | Validation error (negative amount, invalid method, etc.) — show `errors[0]` |

---

## 9. Frontend integration checklist

- [ ] `AdminController.Payments()` — replace `MockData.GetPayments()` with the list endpoint
      (new `IAdminPaymentsService` with `GetPaymentsAsync(request)`, `GetPaymentDetailAsync(id)`,
      `GetPaymentStatsAsync()`, `CreateManualPaymentAsync(request)`, `RefundPaymentAsync(id, reason)`)
- [ ] `AdminController.PaymentsDetails(id)` — load the detail endpoint instead of mock
- [ ] Manual payment modal — post to `POST /admin/payments/manual`, refresh the table on success
- [ ] Ads flow — restrict the "خدمة إعلانية" option to clinics from `GET /admin/ads/eligible-clinics`;
      show a localized message when a clinic is not on the Advanced plan
