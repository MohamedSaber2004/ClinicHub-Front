# Superadmin Payments Page — Frontend Integration Guide

This guide explains how to wire the **Superadmin Payments page** (`Admin/Payments` —
الدفعات والمعاملات المالية) in the web dashboard to the real backend endpoints.
The page currently renders mock data (`MockData.GetPayments()` / `GetPaymentStats()` /
`GetPaymentDetail(id)`) and must be replaced with real API calls.

All endpoints are **already implemented and deployed** (migration `AddAdminPaymentsSupport`
applied). The frontend only needs to consume them.

---

## 1. Endpoints at a glance

| # | Method | Route | Purpose |
|---|--------|-------|---------|
| 1 | `GET` | `/api/v1/admin/payments` | Payments table (filters + search + pagination) |
| 2 | `GET` | `/api/v1/admin/payments/{id}` | Payment detail (payer card, item name, timeline) |
| 3 | `GET` | `/api/v1/admin/payments/stats` | 4 KPI stat cards |
| 4 | `POST` | `/api/v1/admin/payments/manual` | Register a manual payment (دفعة يدوية) |
| 5 | `POST` | `/api/v1/admin/payments/{id}/refund` | Refund a payment |
| 6 | `GET` | `/api/v1/admin/ads/eligible-clinics` | Clinics allowed to buy ads (payer dropdown) |
| 7 | `GET` | `/api/v1/admin/ads/packages` | Available ads packages (pricing for the ads modal) |
| 8 | `POST` | `/api/v1/admin/ads/orders` | Create an ads order → returns Paymob checkout URL |

---

## 2. Common conventions

- **Base URL:** `/api/v1`
- **Auth:** `Authorization: Bearer <token>` — every endpoint requires the **SuperAdmin** role
  (`UserType.SuperAdmin`). Otherwise `401`.
- **Language:** `Accept-Language: ar` (default) — user-facing messages come localized (Arabic).
- **Response wrapper** — every response is `ApiResponse<T>`:

```json
{ "success": true, "data": { }, "message": "string", "errors": [], "statusCode": 200 }
```

Read `data` (may be `null` on errors) and `message` (localized) / `errors[0]` (validation).

- **Pagination wrapper** for the list (`PagginatedResult<T>`):

```json
{
  "items": [],
  "totalCount": 34,
  "totalPages": 2,
  "pageNumber": 1,
  "pageSize": 20,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## 3. Enums — what the backend sends / expects

### PaymentType (النوع)

| Value | Name | Arabic label (UI) |
|-------|------|-------------------|
| `0` | `Appointment` | موعد مريض |
| `1` | `Subscription` | اشتراك عيادة |
| `2` | `Ads` | خدمة إعلانية |

### PaymentStatus (الحالة)

| Value | Name | Arabic label |
|-------|------|--------------|
| `0` | `Pending` | معلق |
| `1` | `Paid` | ناجح |
| `2` | `Failed` | فاشل |
| `3` | `Refunded` | مسترد |

> **Note:** the backend also has an internal `Processing` (4) state for payments
> initiated at Paymob but not yet confirmed. The API **maps it to `Pending` (0)** before
> sending it, so the frontend only ever sees `0..3`.

### PaymentMethod (طريقة الدفع) ⚠️ IMPORTANT

Only **two** payment methods are activated on the platform right now
(**Cash** and **Paymob wallet**). The enum sent by the backend is:

| Value | Name | Arabic label |
|-------|------|--------------|
| `0` | `Cash` | نقدي |
| `1` | `PaymobWallet` | Paymob - محفظة إلكترونية |

> **Deviation from the original spec:** `BankTransfer (1)` and `PaymobCard (2)` are
> **not** included. The manual-payment modal must offer only **نقدي** and
> **Paymob محفظة**. Do not send any other `method` value to the backend.

---

## 4. Endpoint details

### 4.1 List — `GET /api/v1/admin/payments`

Query parameters:

| Param | Type | Notes |
|-------|------|-------|
| `PageNumber` | int | default `1` |
| `PageSize` | int | default `20`, max `100` |
| `Type` | int | `0` / `1` / `2` — omit for all |
| `Status` | int | `0..3` (values above) |
| `Method` | int | `0` (Cash) / `1` (PaymobWallet) |
| `FromDate` | date | `YYYY-MM-DD` — filters by payment creation date |
| `ToDate` | date | `YYYY-MM-DD` (inclusive) |
| `SearchTerm` | string | matches payer name (patient or clinic), `code`, or `refNumber` |

Example call (appointment payments, paid, last 2 weeks, search "محمد"):

```
GET /api/v1/admin/payments?PageNumber=1&PageSize=20&Type=0&Status=1&FromDate=2026-07-20&ToDate=2026-08-02&SearchTerm=محمد
```

Each `items[]` row:

```json
{
  "id": "8f3b...guid",
  "code": "#P-018",
  "type": 1,
  "payer": "مجمع عيادات السلام الطبي",
  "amount": 1500.00,
  "currency": "EGP",
  "method": 1,
  "status": 1,
  "date": "2026-07-01T10:30:00Z",
  "refNumber": "PM-2026-0018"
}
```

| UI column | Field |
|-----------|-------|
| `#` | `code` |
| الدافع | `payer` |
| النوع | `type` (badge: 0 موعد مريض / 1 اشتراك عيادة / 2 خدمة إعلانية) |
| المبلغ | `amount` + `currency` |
| طريقة الدفع | `method` (0 نقدي / 1 Paymob محفظة) |
| الحالة | `status` (0 معلق / 1 ناجح / 2 فاشل / 3 مسترد) |
| التاريخ | `date` |
| رقم مرجعي | `refNumber` (e.g. `PM-2026-0018`) |

### 4.2 Detail — `GET /api/v1/admin/payments/{id}`

Returns everything the detail page needs (payer card + item name + timeline):

```json
{
  "id": "guid",
  "code": "#P-007",
  "type": 0,
  "payer": "محمد عمر (مريض)",
  "payerType": "Patient",
  "payerEmail": "mohamed@email.com",
  "payerPhone": "01111122222",
  "itemName": "كشف موعد",
  "amount": 200.00,
  "method": 1,
  "transactionId": "a1b2c3d4e5f6",
  "refNumber": "PM-2026-0007",
  "status": 1,
  "date": "2026-07-03T10:30:00Z",
  "notes": "",
  "timeline": [
    { "date": "2026-07-03T10:32:00Z", "text": "تم تأكيد الدفع واستلام المبلغ", "marker": "success" },
    { "date": "2026-07-03T10:30:00Z", "text": "تم إنشاء المعاملة", "marker": "info" }
  ]
}
```

Notes:

- `payerType` is `"Patient"` for appointment payments, `"Clinic"` for subscription/ads payments.
- `itemName` is already localized by the backend:
  - Appointment → `كشف موعد`
  - Subscription → `اشتراك سنوي - 2026` or `اشتراك شهري - 2026` (period label + year)
  - Ads → `خدمة إعلانية`
- `timeline` is **ordered newest first** and can contain up to 3 entries with
  `marker` ∈ `info` (created) / `success` (paid) / `danger` (refunded). Render as a
  timeline component with the marker as color.
- `transactionId` = Paymob transaction id (online payments) or the bank-transfer
  reference (manual payments). `null` for unpaid payments.

### 4.3 Stats — `GET /api/v1/admin/payments/stats`

Optional query param `Type` (`0/1/2`) filters **all** values to one payment type
(omit for the global dashboard cards).

```json
{
  "todayRevenue": 12400.00,
  "subscriptionsRevenue": 5000.00,
  "appointmentsRevenue": 2400.00,
  "adsRevenue": 5000.00,
  "pendingCount": 3,
  "successCount": 120,
  "failedCount": 5,
  "refundedCount": 2
}
```

KPI card mapping (4 cards):

| Card | Field |
|------|-------|
| إيرادات الشهر | `todayRevenue` |
| إيرادات الاشتراكات | `subscriptionsRevenue` |
| إيرادات المواعيد | `appointmentsRevenue` |
| إيرادات الإعلانات | `adsRevenue` |

> **Monthly stats (since `2026-08`):** the frontend always sends
> `FromDate=YYYY-MM-01&ToDate=YYYY-MM-<last day>` (month selector, defaults to the
> current month). When both dates are present, **all four revenue fields are
> computed within that date range** (payment date in range, status = ناجح only —
> refunds excluded), and `todayRevenue` acts as the period's total revenue.
> `pendingCount` includes pending + in-progress payments.
>
> **Backward compatible:** when `FromDate`/`ToDate` are omitted, the original
> behavior applies (`todayRevenue` = paid today, others = all-time totals).
> Full backend spec: `docs/superadmin-monthly-stats-api.md`.

### 4.4 Manual payment — `POST /api/v1/admin/payments/manual`

Registers a payment immediately as **successful** (used for cash / offline confirmations).

Request body:

```json
{
  "payerId": "00000001-0000-0000-0000-000000000001",
  "type": 1,
  "amount": 5000,
  "method": 0,
  "refNumber": "TRF-2026-8821",
  "notes": "دفعة نقدية من العيادة"
}
```

| Field | Notes |
|-------|-------|
| `payerId` | **Clinic id** (from the payer dropdown). Only clinics are supported. |
| `type` | **`1` (Subscription) or `2` (Ads) only** — `0` (Appointment) is rejected with a localized message. |
| `amount` | > 0 |
| `method` | `0` (نقدي) or `1` (Paymob محفظة) |
| `refNumber` | optional, ≤ 50 chars; if omitted the backend generates one (`PM-2026-xxxx`) |
| `notes` | optional, ≤ 500 chars |

Success → **`201`** with the created payment row (same shape as a list item).
When `type = 2` (Ads) and the clinic is **not** on the Advanced plan → **`403`** with
message `لا يمكن شراء الخدمات الإعلانية إلا للباقات المتقدمة`.

### 4.5 Refund — `POST /api/v1/admin/payments/{id}/refund`

Request body:

```json
{ "reason": "تم استرداد المبلغ بناءً على طلب العيادة" }
```

Behavior:

- Online (Paymob) payments → backend calls Paymob refund API first; only on success
  the status becomes `Refunded` (`3`). If the Paymob refund fails → `400`.
- Cash/manual payments → refunded directly.
- Already-refunded payment → `400` (`تم استرداد هذا الدفع مسبقاً`).
- Missing payment → `404`.

Success → **`200`** with `true`.

### 4.6 Ads eligibility — `GET /api/v1/admin/ads/eligible-clinics`

Returns clinics that may purchase ads: **active subscription AND Advanced plan**:

```json
[
  { "id": "guid", "name": "مجمع عيادات السلام الطبي", "email": "clinic@mail.com", "phone": "01122233344" },
  { "id": "guid", "name": "عيادة النور", "email": null, "phone": "01000000000" }
]
```

Use this to fill the **payer dropdown** when the manual-payment modal has
`type = 2` (خدمة إعلانية). For `type = 1` (اشتراك عيادة) use the regular clinics list
(`GET /api/v1/admin/dashboard/clinics`) instead.

### 4.7 Ads packages — `GET /api/v1/admin/ads/packages`

Lists the purchasable ads packages (homepage banner, featured placement, …):

```json
[
  {
    "id": "guid",
    "name": "Homepage Banner",
    "nameAr": "بانر الصفحة الرئيسية",
    "description": "Featured banner on the homepage for one month.",
    "descriptionAr": "بانر مميز في الصفحة الرئيسية لمدة شهر.",
    "price": 500.00,
    "durationDays": 30,
    "isActive": true
  }
]
```

> **Important:** packages are managed directly in the `AdPackages` DB table (there is
> **no seeder and no CRUD endpoint**). If the table is empty, insert the packages
> through SQL or the DB admin panel, otherwise the ads modal has nothing to show.

### 4.8 Ads order — `POST /api/v1/admin/ads/orders`

Creates an ads payment and returns the **Paymob checkout URL** to redirect to.

Request body:

```json
{
  "clinicId": "00000001-0000-0000-0000-000000000001",
  "adPackageId": "guid",
  "durationDays": 60,
  "returnUrl": "https://dashboard.doctory.com/Home/PaymentResult"
}
```

Success → **`201`**:

```json
{
  "paymentId": "guid",
  "refNumber": "PM-2026-0035",
  "amount": 500.00,
  "currency": "EGP",
  "status": 0,
  "paymobRedirectUrl": "https://accept.paymob.com/unifiedcheckout/?publicKey=...&clientSecret=...",
  "paymobPaymentKey": "..."
}
```

Integration flow:

1. Load eligible clinics + packages → user picks clinic, package, duration.
2. `POST /admin/ads/orders` → get `paymobRedirectUrl`.
3. Redirect the user to `paymobRedirectUrl` (new tab/window).
4. After payment, Paymob redirects to `returnUrl`; the backend webhook marks the
   payment `Paid` (`1`) automatically. The payment then appears in the payments
   table with `type = 2`.
5. **403** is returned when the clinic is not on the Advanced plan →
   show `message`: `لا يمكن شراء الخدمات الإعلانية إلا للباقات المتقدمة`.

---

## 5. UI wiring checklist (replace mocks)

- [ ] `AdminController.Payments()` — replace `MockData.GetPayments()` with
      `GET /admin/payments` (keep PageNumber/PageSize/Type/Status/Method/FromDate/ToDate/SearchTerm
      from the page filters).
- [ ] KPI cards — replace `MockData.GetPaymentStats()` with `GET /admin/payments/stats`.
- [ ] `AdminController.PaymentsDetails(id)` — replace `MockData.GetPaymentDetail(id)` with
      `GET /admin/payments/{id}` and render the timeline.
- [ ] Manual payment modal ("تسجيل دفعة يدوية") — `POST /admin/payments/manual`;
      on `201` refresh the table + stats. Payer dropdown depends on `type`:
      - `اشتراك عيادة` → clinics list (`/admin/dashboard/clinics`)
      - `خدمة إعلانية` → `GET /admin/ads/eligible-clinics` only
      - Do **not** offer موعد مريض (rejected by backend)
      - Method options: only نقدي and Paymob محفظة
- [ ] Ads flow (if the page creates ads orders):
      - packages: `GET /admin/ads/packages`
      - order + redirect: `POST /admin/ads/orders` → `paymobRedirectUrl`
      - show localized message from `403` when clinic is not on Advanced plan
- [ ] Refund action — `POST /admin/payments/{id}/refund` with `{ reason }`;
      on success refresh the row + stats.

## 6. Errors

| Code | Meaning | Frontend action |
|------|---------|-----------------|
| `401` | Missing/invalid/expired admin token | redirect to login |
| `403` | Not SuperAdmin, or ads purchase by non-Advanced clinic | show `message` in a toast |
| `404` | Payment / clinic / ads package not found | show "not found" state |
| `400` | Validation error (`errors[0]`) or refund failed | show `errors[0]` / `message` in a toast |

Error body: `{ "success": false, "data": null, "message": "...", "errors": ["..."], "statusCode": 400 }`

## 7. Data notes

- `code` (`#P-xxx`) and `refNumber` (`PM-2026-xxxx`) are **auto-generated by the backend**
  for new payments. Existing payments were backfilled by a one-time SQL update.
- The unique filter on `refNumber` means every payment always has a distinct reference.
- Payments list is ordered newest-first (`date` desc).
- Localization: request with `Accept-Language: ar` (or `en`) — `itemName`, `message`,
  and timeline texts come localized automatically.
