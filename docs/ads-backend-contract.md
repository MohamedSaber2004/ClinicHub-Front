# Ads Feature — Backend Contract (missing endpoints)

**Status:** required — the web dashboard ads pages (`Clinic/Marketing`, `Admin/Ads`) consume
these endpoints. None of them exist yet. Full business rules: `docs/superadmin-ads-feature.md`.

Conventions (identical to existing admin/payments APIs):
- Base URL `/api/v1`; `ApiResponse<T>` envelope; `Accept-Language: ar` → localized messages.
- Auth: `Bearer` + role — ClinicOwner (clinic endpoints), SuperAdmin (admin endpoints), none (public).

---

## 1. Clinic owner endpoints

### 1.1 My ads — `GET /api/v1/clinics/{clinicId}/ads`

Clinic's own ads (history). Optional `Status` (`0..3`) filter.

Response `data` (list):

```json
[
  {
    "id": "guid",
    "packageId": "guid",
    "packageNameAr": "بانر الصفحة الرئيسية",
    "durationDays": 60,
    "amount": 1000.00,
    "currency": "EGP",
    "status": 1,
    "startDate": "2026-08-01T00:00:00Z",
    "endDate": "2026-09-30T00:00:00Z",
    "createdAt": "2026-08-01T09:00:00Z"
  }
]
```

### 1.2 Packages — `GET /api/v1/ads/packages`

Active (`IsActive = true`) ad packages — same `AdPackageDto` shape as the admin packages
endpoint (`id`, `name`, `nameAr`, `description`, `descriptionAr`, `price`, `durationDays`,
`isActive`).

### 1.3 Create ad order — `POST /api/v1/clinics/{clinicId}/ads/orders`

Request:

```json
{
  "adPackageId": "guid",
  "durationDays": 60,
  "returnUrl": "https://dashboard.doctory.com/Home/PaymentResult"
}
```

Rules:
- **Eligibility gate:** clinic must have an active subscription AND be on the Advanced plan,
  else `403` `لا يمكن شراء الخدمات الإعلانية إلا للباقات المتقدمة`.
- `durationDays` must be a whole positive multiple of `package.DurationDays`, else `400`.
- Creates ad record `status = 0` (pending-payment) + payment record, returns Paymob checkout URL.
- On Paymob success (webhook) → ad becomes `status = 1`, `startDate = now`,
  `endDate = now + durationDays`, payment `Paid` → appears in admin payments (type 2).

Success `201` — same `AdsOrderResponseDto` shape as the admin endpoint
(`paymentId`, `refNumber`, `amount`, `currency`, `status`, `paymobRedirectUrl`,
`paymobPaymentKey`).

## 2. Superadmin endpoints

### 2.1 All ads — `GET /api/v1/admin/ads`

Paginated (`PageNumber`, `PageSize` max 100), optional `Status` filter (`0..3`).

Response `data` — `PagginatedResult<AdminAdDto>` where each item adds clinic info:

```json
{
  "items": [
    {
      "id": "guid",
      "clinicId": "guid",
      "clinicName": "مجمع عيادات السلام الطبي",
      "packageId": "guid",
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
}
```

### 2.2 Deactivate — `POST /api/v1/admin/ads/{id}/deactivate`

Admin moderation. Body `{ "reason": "محتوى مخالف" }` (optional, ≤ 200 chars).
- Sets `status = 3` (ملغي) → removed from mobile display immediately.
- Already deactivated/expired → `400`; missing → `404`. Success `200` `true`.

### 2.3 Packages CRUD — `GET/POST/PUT/DELETE /api/v1/admin/ads/packages`

Replaces "seed via SQL" (there is currently no CRUD — docs said insert manually).

- `GET` — all packages (incl. inactive — admin needs them).
- `POST` body: `{ name, nameAr, description, descriptionAr, price, durationDays, isActive }` → `201` `AdPackageDto`.
- `PUT /{id}` — same body, full update → `200` `AdPackageDto`.
- `DELETE /{id}` → `200` `true`. If the package has ads → `409` localized message
  (suggest: set `isActive = false` instead of deleting).
- `price` > 0, `durationDays` > 0 → else `400`.

## 3. Public (mobile app)

### 3.1 Active ads — `GET /api/v1/public/ads/active`

No auth. Returns currently-live ads (`status = 1` AND `endDate >= now`), newest first:

```json
[
  {
    "id": "guid",
    "clinicId": "guid",
    "clinicName": "مجمع عيادات السلام الطبي",
    "clinicLogoUrl": "/files/clinic-logo.png",
    "packageId": "guid",
    "packageNameAr": "بانر الصفحة الرئيسية",
    "title": null,
    "startDate": "2026-08-01T00:00:00Z",
    "endDate": "2026-09-30T00:00:00Z"
  }
]
```

## 4. Cross-cutting rules

- **Manual cash ad payment:** `POST /api/v1/admin/payments/manual` with `type = 2` already
  records the payment as successful; **additionally** it must activate the clinic's most recent
  `status = 0` ad (same instant-activation rules: `startDate = now`, `endDate = now + duration`).
- **Refund:** `POST /api/v1/admin/payments/{id}/refund` on an ads payment (type 2) must also set
  the linked ad to `status = 3` (ملغي).
- **Ad activation idempotency:** webhook/manual payment must not double-extend `endDate`.
- **Deletion:** no ad-delete endpoint (deactivate only) — keeps payment/ad history intact.

## Acceptance criteria

1. Clinic with active Advanced plan: buy flow returns Paymob URL; webhook → ad active in
   `GET /api/v1/public/ads/active`; row in admin ads list + admin payments (type 2).
2. Clinic without Advanced plan: `403` with the Arabic message on both clinic and admin order
   endpoints.
3. Manual cash payment (type 2) activates the clinic's pending ad.
4. Deactivate → ad gone from public endpoint instantly, status 3 in lists.
5. Expired ads (`endDate < now`) never appear in the public endpoint.
6. Package CRUD works; deleting a package used by ads → `409` guidance message.
