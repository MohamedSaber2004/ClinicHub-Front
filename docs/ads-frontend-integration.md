# Ads Feature — Frontend Integration Guide (الإعلانات)

Guide for the web dashboards to integrate the ads feature:

- **Clinic owner dashboard** — `Clinic/Marketing` (أدوات تسويقية): my ads, buy ads, pricing.
- **Superadmin dashboard** — `Admin/Ads` (إدارة الإعلانات): manage all ads + packages; plus the
  existing "طلب خدمة إعلانية" flow on `Admin/Payments`.
- **Mobile app** (separate product) — consumes the public endpoint; the web repo only specs it.

Backend is fully implemented and deployed with the `AddAdsOrderFlow` migration.

---

## 1. Conventions (same as every other ClinicHub API)

| Thing | Value |
|---|---|
| Base URL | `/api/v1` |
| Response envelope | `ApiResponse<T>` (see below) |
| Localization | `Accept-Language: ar` → Arabic messages (default Arabic) |
| Auth | `Authorization: Bearer <token>` |
| Roles | `ClinicOwner` for clinic endpoints, `SuperAdmin` for admin endpoints, none for public |

### Envelope shape

Every endpoint returns a JSON body with this envelope:

```json
{
  "success": true,
  "errors": {},
  "data": { ... },
  "message": "string | null",
  "statusCode": 200
}
```

- On success: `success = true`, `data` holds the payload, `message` may hold a localized
  success message (e.g. `"تم إلغاء الإعلان بنجاح"`).
- On failure: `success = false`, `message` holds the localized error text, `errors` may hold
  per-field validation errors, `statusCode` mirrors the HTTP status.

**Always read `data` from `response.data`, never use the raw response directly.**

---

## 2. Ad statuses & lifecycle

`status` is an integer on every ad list item:

| Value | Name | Meaning | Trigger |
|---|---|---|---|
| `0` | pending-payment (معلق الدفع) | Order created, waiting for payment | `POST .../ads/orders` |
| `1` | active (نشط) | Paid — visible in the mobile app | Paymob webhook success OR admin manual cash payment (type 2) |
| `2` | expired (منتهي) | `EndDate` passed | automatic by date (no API call) |
| `3` | deactivated (ملغي) | Taken down by admin | `POST /api/v1/admin/ads/{id}/deactivate` |

Lifecycle rules the UI should respect:

- **Instant activation after payment** — no admin approval step exists. The moment Paymob
  confirms (or the admin records cash), the ad jumps `0 → 1` with `startDate = now`,
  `endDate = now + durationDays`.
- **Deactivated ads cannot be re-activated** — no such endpoint exists. Status `3` is final.
- **Expired ads stay in history** — they never disappear from lists, they just become `2`.
- **Paid ads keep running even if the clinic's subscription lapses** — already-paid content is
  not revoked retroactively.

---

## 3. Business rules (hard backend gates — mirror them in the UI)

### 3.1 Eligibility (only for buying)

- Buying requires: **active subscription AND Advanced plan**.
- Backend enforces this on both order endpoints → `403` with
  `لا يمكن شراء الخدمات الإعلانية إلا للباقات المتقدمة` (`Payments.AdsNotEligible`).
- **Frontend proxy for "Advanced":** `PlanFeature.MarketingTools` + `HasActivePlan` — the same
  gate the sidebar uses today. When the clinic lacks it:
  - `Clinic/Marketing` shows the **upsell card** (ترقية الباقة → `Clinic/MySubscription`).
  - The buy button is disabled with an upsell message.
  - The API still returns `403` as the hard gate — handle it gracefully anyway.

### 3.2 Pricing (proportional to duration)

```
amount = package.price × (durationDays / package.durationDays)
```

- `durationDays` must be a **whole positive multiple** of `package.durationDays`.
- Example: package بانر رئيسي = 500 ج.م / 30 يوم → buying 60 days = 1,000 ج.م.
- Backend rejects invalid durations with `400` (`Ads.InvalidDuration`:
  `يجب أن تكون المدة مضاعفاً صحيحاً لمدة الباقة الإعلانية`).
- Validate client-side in the buy modal: `durationDays > 0 && durationDays % package.durationDays === 0`
  and show the live price preview using the formula above.

### 3.3 Manual cash payments (superadmin)

- `POST /api/v1/admin/payments/manual` with `type = 2` records a successful cash payment **and
  automatically activates the clinic's most recent pending-payment ad** (`0 → 1`).
- So on `Admin/Payments` the "طلب خدمة إعلانية" flow is: create the order (paymob URL) → admin
  marks it paid manually → the ad becomes active. No extra call needed.
- **Idempotent** — a second manual payment only activates another ad if one is still pending.

### 3.4 Refund (superadmin)

- `POST /api/v1/admin/payments/{id}/refund` on an ads payment (`type = 2`) **also sets the linked
  ad to `3` (ملغي)** automatically. The UI should refresh the ads list after a refund.

---

## 4. Clinic owner endpoints (`Clinic/Marketing`)

Auth: `Bearer` + role `ClinicOwner`. All endpoints below are available to every clinic owner
(no plan gate at the HTTP level — lists and pricing are visible so the upsell card can render).

### 4.1 My ads — `GET /api/v1/clinics/{clinicId}/ads`

Optional query: `?Status=0|1|2|3` (filter by status; omit for all).

```json
{
  "success": true,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "packageId": "d2e2f0c8-0000-0000-0000-000000000001",
      "packageNameAr": "بانر الصفحة الرئيسية",
      "durationDays": 60,
      "amount": 1000.00,
      "currency": "EGP",
      "status": 1,
      "startDate": "2026-08-01T00:00:00Z",
      "endDate": "2026-09-30T00:00:00Z",
      "createdAt": "2026-08-01T09:00:00Z"
    }
  ],
  "message": null,
  "statusCode": 200
}
```

UI notes:
- List ordered newest-first. Render `packageNameAr`, amount (`+ " " + currency`), period
  (`startDate → endDate`), and a badge per `status` (معلق الدفع / نشط / منتهي / ملغي).
- `status` here is the **source of truth**; expired (`2`) rows keep `endDate` in the past — do
  not compute status client-side, just render it.
- `clinicId` = the clinic of the logged-in owner (take it from the auth payload / route).

### 4.2 Packages (pricing) — `GET /api/v1/ads/packages`

Returns **only active** packages, ordered by `sortOrder`:

```json
{
  "success": true,
  "data": [
    {
      "id": "d2e2f0c8-0000-0000-0000-000000000001",
      "name": "Main Banner",
      "nameAr": "بانر الصفحة الرئيسية",
      "description": "Homepage main banner",
      "descriptionAr": "بانر رئيسي في الصفحة الرئيسية",
      "price": 500.00,
      "durationDays": 30,
      "isActive": true
    }
  ],
  "message": null,
  "statusCode": 200
}
```

UI notes:
- Render `nameAr ?? name`, `descriptionAr ?? description`, `price + " ج.م"` (or currency from
  the payment response), `durationDays`.
- The buy modal lists these packages; on package select show duration options that are whole
  multiples of `durationDays` (e.g. for a 30-day package: 30 / 60 / 90 ...).

### 4.3 Create ad order — `POST /api/v1/clinics/{clinicId}/ads/orders`

Request:

```json
{
  "adPackageId": "d2e2f0c8-0000-0000-0000-000000000001",
  "durationDays": 60,
  "returnUrl": "https://dashboard.doctory.com/Home/PaymentResult"
}
```

Creates the ad (`status = 0`) + a payment record, then returns the Paymob checkout URL:

```json
{
  "success": true,
  "data": {
    "paymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "refNumber": "PM-2026-A1B2C3",
    "amount": 1000.00,
    "currency": "EGP",
    "status": 2,
    "paymobRedirectUrl": "https://accept.paymob.com/...",
    "paymobPaymentKey": "paymob-payment-key"
  },
  "message": null,
  "statusCode": 201
}
```

**Payment flow (must be implemented):**

1. User picks package + duration in the modal → POST the order.
2. Redirect the browser to `data.paymobRedirectUrl` (Paymob hosted page).
3. Paymob redirects back to `returnUrl` on completion (use `?success=true/false`).
4. **Do NOT activate the ad client-side.** Activation happens server-side when the Paymob
   webhook fires (a few seconds later).
5. Refetch `GET .../ads` (or filter `?Status=1`) after returning to the dashboard — the new ad
   should appear as `status: 1` with proper `startDate`/`endDate`. Poll up to ~10s if needed.

Possible errors:

| Status | Message (ar) | Meaning / UI action |
|---|---|---|
| `400` | `الباقة الإعلانية غير موجودة` / `هذه الباقة الإعلانية غير متاحة حالياً` | Stale package — refetch packages |
| `400` | `يجب أن تكون المدة مضاعفاً صحيحاً لمدة الباقة الإعلانية` | Bad duration — clamp options client-side |
| `400` | `المستخدم المسؤول عن الدفع غير موجود` | Clinic has no payer user — support case |
| `403` | `لا يمكن شراء الخدمات الإعلانية إلا للباقات المتقدمة` | Not eligible → show upsell card |
| `404` | `العيادة غير موجودة` | Wrong/mismatched `clinicId` |

---

## 5. Superadmin endpoints (`Admin/Ads` + `Admin/Payments`)

Auth: `Bearer` + role `SuperAdmin`.

### 5.1 All ads — `GET /api/v1/admin/ads`

Query: `?PageNumber=1&PageSize=20&Status=0|1|2|3` (`PageSize` max 100; status optional).

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "clinicId": "11111111-1111-1111-1111-111111111111",
        "clinicName": "مجمع عيادات السلام الطبي",
        "packageId": "d2e2f0c8-0000-0000-0000-000000000001",
        "packageNameAr": "بانر الصفحة الرئيسية",
        "durationDays": 60,
        "amount": 1000.00,
        "currency": "EGP",
        "status": 1,
        "startDate": "2026-08-01T00:00:00Z",
        "endDate": "2026-09-30T00:00:00Z",
        "createdAt": "2026-08-01T09:00:00Z"
      }
    ],
    "totalCount": 25,
    "totalPages": 2,
    "pageNumber": 1,
    "pageSize": 20,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "message": null,
  "statusCode": 200
}
```

UI notes (Tab 1 — إدارة الإعلانات):
- Render a table: clinic, package, duration, amount + currency, period, status badge, created date.
- Status filter (tab pills or dropdown) → refetch with `Status`.
- Pagination controls from `pageNumber/pageSize/totalCount/hasNextPage/hasPreviousPage`.

### 5.2 Deactivate an ad — `POST /api/v1/admin/ads/{id}/deactivate`

Request body (reason optional, ≤ 200 chars):

```json
{ "reason": "محتوى مخالف" }
```

Success → `200`, `data: true`, message `تم إلغاء الإعلان بنجاح`.

Rules:
- Only `0` (pending) and `1` (active) ads can be deactivated.
- Already `3` (deactivated) or `2` (expired) → `400` `هذا الإعلان مُلغى أو منتهي بالفعل`.
- Missing id → `404` `الإعلان غير موجود`.
- Deactivated ads disappear from the mobile public endpoint **immediately** and can never be
  reactivated — confirm before calling (modal with reason input is a good UX).

### 5.3 Packages CRUD (Tab 2 — إدارة الباقات الإعلانية)

Replaces the old "seed via SQL" workflow. All packages include inactive ones so the admin can
re-enable.

**List — `GET /api/v1/admin/ads/packages`** (no query params)

```json
{
  "success": true,
  "data": [
    {
      "id": "d2e2f0c8-0000-0000-0000-000000000001",
      "name": "Main Banner",
      "nameAr": "بانر الصفحة الرئيسية",
      "description": "Homepage main banner",
      "descriptionAr": "بانر رئيسي في الصفحة الرئيسية",
      "price": 500.00,
      "durationDays": 30,
      "isActive": true
    }
  ],
  "message": null,
  "statusCode": 200
}
```

**Create — `POST /api/v1/admin/ads/packages`** → `201`, returns the created package.

**Update — `PUT /api/v1/admin/ads/packages/{id}`** → `200`, returns the updated package.

Both use the same body:

```json
{
  "name": "Main Banner",
  "nameAr": "بانر الصفحة الرئيسية",
  "description": "Homepage main banner",
  "descriptionAr": "بانر رئيسي في الصفحة الرئيسية",
  "price": 500.00,
  "durationDays": 30,
  "isActive": true
}
```

Validation: `name` required, `price > 0`, `durationDays > 0` → else `400`.

**Delete — `DELETE /api/v1/admin/ads/packages/{id}`** → `200`, `data: true`.

- If the package is referenced by any ad → `409` with
  `هذه الباقة مستخدمة في إعلانات قائمة. يُنصح بتعطيلها بدلاً من حذفها` — in the UI, fall back
  to an "إلغاء التفعيل" (set `isActive: false` via PUT) suggestion.

### 5.4 Orders & manual cash payments (existing `Admin/Payments` page)

Unchanged page, but the ads integration now also activates ads:

1. **Create order:** `POST /api/v1/admin/ads/orders` (same body as 4.3 plus `clinicId`:
   `{ "clinicId", "adPackageId", "durationDays", "returnUrl" }`) — creates ad `status 0` + payment,
   returns the Paymob URL. Use the clinic dropdown from
   `GET /api/v1/admin/ads/eligible-clinics` (only Advanced-plan clinics with active subscriptions).
2. **Record cash:** `POST /api/v1/admin/payments/manual` with
   `{ "payerId": clinicId, "type": 2, "amount", "method", "refNumber", "notes" }` → payment is
   `Paid` **and the clinic's most recent pending ad becomes active instantly**.
3. **Refund:** `POST /api/v1/admin/payments/{id}/refund` → also flips the linked ad to `3`.
   Refresh the ads table after refunding.

---

## 6. Public endpoint (mobile app, for reference)

No auth. `GET /api/v1/public/ads/active` → currently-live ads (`status = 1` AND `endDate >= now`),
newest first. The web dashboards do NOT need to call this.

```json
{
  "success": true,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "clinicId": "11111111-1111-1111-1111-111111111111",
      "clinicName": "مجمع عيادات السلام الطبي",
      "clinicLogoUrl": "/files/clinic-logo.png",
      "packageId": "d2e2f0c8-0000-0000-0000-000000000001",
      "packageNameAr": "بانر الصفحة الرئيسية",
      "title": null,
      "startDate": "2026-08-01T00:00:00Z",
      "endDate": "2026-09-30T00:00:00Z"
    }
  ],
  "message": null,
  "statusCode": 200
}
```

---

## 7. Quick integration checklist

### `Clinic/Marketing` (clinic owner)

- [ ] Load `GET /clinics/{clinicId}/ads` → إعلاناتي list with status badges + dates + amounts + empty state.
- [ ] Load `GET /ads/packages` → pricing cards.
- [ ] Eligibility gate: if not `PlanFeature.MarketingTools` or no active plan → upsell card, buy disabled. (API still 403s.)
- [ ] Buy modal: package select → duration options (whole multiples) → live price preview (`price × duration/package.durationDays`).
- [ ] `POST /clinics/{clinicId}/ads/orders` → redirect to `paymobRedirectUrl` → on return refetch ads (`?Status=1`), poll briefly until the ad shows active.
- [ ] Handle `403` (upsell) and `400` (invalid duration) responses gracefully.

### `Admin/Ads` (superadmin)

- [ ] Tab 1: `GET /admin/ads` (paginated + status filter) → table + deactivate button per active/pending row.
- [ ] Deactivate: confirm modal with optional reason → `POST /admin/ads/{id}/deactivate` → refetch; show success/failure messages from the response.
- [ ] Tab 2: `GET /admin/ads/packages` → list with active toggle; create/edit modal (`POST`/`PUT`); delete with `409` fallback message (suggest deactivate instead).

### `Admin/Payments` (superadmin)

- [ ] "طلب خدمة إعلانية" modal: eligible clinics dropdown (`GET /admin/ads/eligible-clinics`) + packages + duration → `POST /admin/ads/orders` → Paymob URL.
- [ ] Manual cash (type 2) activates the clinic's pending ad — no extra action.
- [ ] Refunding an ads payment deactivates its ad — refresh the ads table afterward.

---

## 8. Error codes summary

| Code | When | Typical message (ar) |
|---|---|---|
| `400` | invalid duration / inactive package / invalid manual type / double deactivate | `يجب أن تكون المدة مضاعفاً صحيحاً...` |
| `403` | not Advanced plan / no active subscription | `لا يمكن شراء الخدمات الإعلانية إلا للباقات المتقدمة` |
| `404` | ad/package/clinic not found | `الإعلان غير موجود` / `الباقة الإعلانية غير موجودة` |
| `409` | deleting a package in use | `هذه الباقة مستخدمة في إعلانات قائمة...` |

Every error body still uses the `ApiResponse<T>` envelope: `{ success: false, message, errors, statusCode }`.
