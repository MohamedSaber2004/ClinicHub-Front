# 📢 Ads — Backend Status & API Guide (for Frontend)

> **What is already implemented on the backend** for the ads feature, and how the frontend should consume it.
> Base URL for all endpoints: `{base}/api/v1` (API version `1.0`).

---

## ✅ What's Done

| Feature | Status |
|---|---|
| Ad packages (list / CRUD for superadmin) | ✅ Implemented |
| Clinic owner creates ad order (online payment via Paymob) | ✅ Implemented |
| Paymob webhook activates the ad (status 0 → 1) | ✅ Implemented |
| Manual cash payment by superadmin activates pending ad | ✅ Implemented |
| Ad **logo upload + storage** (`logoImageUrl` → `ad.imageUrl`) | ✅ Implemented |
| `imageUrl` returned in **all** ad endpoints (null-safe) | ✅ Implemented |
| Public endpoint for the **patient mobile app** (`GET /public/ads/active`) | ✅ Implemented |
| Deactivate ad (superadmin) | ✅ Implemented |

---

## 📦 Ad Statuses

| Value | Meaning | Arabic |
|---|---|---|
| 0 | `PendingPayment` | معلق الدفع |
| 1 | `Active` | نشط |
| 2 | `Expired` | منتهي |
| 3 | `Deactivated` | معطل |

> Only the **Paymob webhook** (online payment) or the **admin cash payment** flips an ad from `0 → 1`. There is no manual/polling activation endpoint.

---

## 🔑 Auth

| Who | Token |
|---|---|
| Clinic owner | `ClinicOwner` bearer token |
| Superadmin | `SuperAdmin` bearer token |
| Patient app (`/public/ads/active`) | **No auth** (`AllowAnonymous`) |

---

## 📍 Endpoints

### 1. `GET {base}/api/v1/ads/packages` — Clinic owner
Active ad packages (only `IsActive = true`).

```json
{ "success": true, "data": [{
  "id": "3f9a0000-0000-0000-0000-000000000001",
  "name": "Featured Slider",
  "nameAr": "شريط مميز",
  "description": null,
  "descriptionAr": null,
  "price": 600.00,
  "durationDays": 14,
  "isActive": true
}] }
```

---

### 2. `GET {base}/api/v1/clinics/{clinicId}/ads?status=0|1|2|3` — Clinic owner
The clinic's ads, newest first. `status` query is optional (no filter when omitted).

```json
{ "success": true, "data": [{
  "id": "5f2c0000-0000-0000-0000-000000000002",
  "packageId": "3f9a0000-0000-0000-0000-000000000001",
  "packageNameAr": "شريط مميز",
  "durationDays": 14,
  "amount": 600.00,
  "currency": "EGP",
  "status": 1,
  "startDate": "2026-08-05T00:00:00",
  "endDate": "2026-08-19T00:00:00",
  "createdAt": "2026-08-05T10:00:00",
  "imageUrl": "clinic-logo-8f3a.png"
}] }
```

---

### 3. `POST {base}/api/v1/clinics/{clinicId}/ads/orders` — Clinic owner (with logo)

**Request body:**
```json
{
  "adPackageId": "3f9a0000-0000-0000-0000-000000000001",
  "durationDays": 14,
  "logoImageUrl": "clinic-logo-8f3a.png",
  "returnUrl": "https://{host}/Clinic/AdPaymentResult"
}
```

| Field | Type | Notes |
|---|---|---|
| `adPackageId` | Guid | required |
| `durationDays` | int | required — must be a **multiple of the package duration** |
| `logoImageUrl` | string \| null | **optional** — relative file name from the upload endpoint |
| `returnUrl` | string \| null | optional — where Paymob redirects after payment (defaults to the dashboard result page) |

**Response `201`:**
```json
{
  "success": true,
  "data": {
    "paymentId": "b7c30000-0000-0000-0000-000000000009",
    "refNumber": "PM-2026-000123",
    "amount": 600.00,
    "currency": "EGP",
    "status": 4,
    "paymobRedirectUrl": "https://accept.paymob.com/unifiedcheckout/?public_key=...",
    "paymobPaymentKey": "eyJhbGciOi...",
    "imageUrl": "clinic-logo-8f3a.png"
  }
}
```
> `status` here is the **payment** status (`4` = Processing). Redirect the user to `paymobRedirectUrl`.

**Errors:** `404` (package/clinic not found), `400` (inactive package / invalid duration / no payer user), `403` (clinic not eligible — no active `AdvancedReports` plan).

---

### 4. `GET {base}/api/v1/admin/ads?PageNumber=&PageSize=&Status=` — Superadmin
Paginated list of **all** ads. `Status` optional. Same ad shape as #2, plus `clinicId` / `clinicName`.

```json
{
  "success": true,
  "data": {
    "items": [{ "id": "...", "clinicId": "...", "clinicName": "مركز القلب التخصصي", "...": "...", "imageUrl": "clinic-logo-8f3a.png" }],
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 1,
    "totalCount": 5,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

---

### 5. `GET {base}/api/v1/admin/ads/eligible-clinics` — Superadmin
Clinics eligible to buy ads (active subscription with `AdvancedReports`). Used by the admin payments modal.

```json
{ "success": true, "data": [{ "id": "...", "name": "مركز القلب التخصصي", "email": "x@y.com", "phone": "0100..." }] }
```

---

### 6. `POST {base}/api/v1/admin/ads/orders` — Superadmin (manual order)
Same request/response as #3 (with `clinicId`), created on behalf of a clinic. `logoImageUrl` accepted when the admin picked a logo.

```json
{
  "clinicId": "2d1a0000-0000-0000-0000-000000000003",
  "adPackageId": "3f9a0000-0000-0000-0000-000000000001",
  "durationDays": 14,
  "logoImageUrl": "clinic-logo-8f3a.png",
  "returnUrl": null
}
```

---

### 7. `POST {base}/api/v1/admin/ads/{id}/deactivate` — Superadmin
```json
{ "reason": "مخالفة شروط العرض" }
```
Response `200` with a localized message.

---

### 8. `GET {base}/api/v1/public/ads/active` — **Patient mobile app** (no auth)
Only `status == 1` ads that are **not yet expired**, newest `endDate` first.

```json
{
  "success": true,
  "data": [{
    "id": "5f2c0000-0000-0000-0000-000000000002",
    "clinicId": "2d1a0000-0000-0000-0000-000000000003",
    "clinicName": "مركز القلب التخصصي",
    "clinicLogoUrl": null,
    "imageUrl": "clinic-logo-8f3a.png",
    "packageId": "3f9a0000-0000-0000-0000-000000000001",
    "packageNameAr": "شريط مميز",
    "title": null,
    "startDate": "2026-08-05T00:00:00",
    "endDate": "2026-08-19T00:00:00"
  }]
}
```

---

## 🖼️ The Logo / `imageUrl` Contract (read carefully)

- **Upload first:** `POST {base}/api/v1/attachments/upload?place=5` (multipart, field name `file`) → returns `fileName` = **relative path** (e.g. `clinic-logo-8f3a.png`).
- Send that `fileName` as `logoImageUrl` in the order body. **Missing/absent → stored as `null` — never a 400.**
- The backend stores it on the ad (`Advertisement.ImageUrl`) and it **survives** activation (webhook/cash), refund, and deactivation.
- **Every** ad response contains an `imageUrl` key:
  - `string` — relative file name → **full URL = `{base}/files/{imageUrl}`** (the backend serves attachments there).
  - `null` — clinic uploaded no logo. ⚠️ **Always expect the key** (it's never missing / never an empty string).

### Mobile app rendering logic
```
IF ad.imageUrl != null:
    render Image(src = "{base}/files/{ad.imageUrl}") as the ad visual
    fallback label: ad.clinicName

ELSE:
    render text badge:
      • Headline:  ad.clinicName
      • Sub-label: ad.packageNameAr
      • Period:    ad.startDate → ad.endDate (YYYY-MM-DD)
```

---

## 💳 Payment Flow (what the frontend must know)

1. Clinic owner POSTs the order → backend creates the ad (`status = 0`) + payment, returns `paymobRedirectUrl`.
2. Frontend redirects the user to Paymob hosted checkout.
3. Paymob redirects back to `returnUrl` after payment.
4. Paymob webhook (`POST {base}/api/v1/payments/webhook`) validates HMAC → marks payment `Paid` → activates the ad (`status = 1`, `startDate = now`, `endDate = startDate + durationDays`).
5. **The frontend must NOT activate ads itself** — just poll `GET /clinics/{clinicId}/ads` (or the public endpoint) to reflect the new status.

---

## ⚠️ Notes & Gotchas

- All responses are wrapped in `ApiResponse<T>`: `{ success, data, message, statusCode, errors }`. Errors are **localized Arabic messages** in `message` / `errors`.
- Default culture is Arabic — set the `Accept-Language` header (`ar` / `en`) for localized messages.
- `durationDays` must be a multiple of the chosen package's `durationDays` (e.g. package = 14 → 14, 28, 42…).
- Clinic eligibility for ads requires an **active subscription with `AdvancedReports` permission** → otherwise `403`.
- API docs (interactive): `{base}/scalar/v1`.

---

## 🔗 Related Docs

- `docs/ads-logo-contract.md` — the logo feature contract (this spec is now fully implemented on the backend)
- `docs/ads-backend-contract.md` — payment/webhook behavior details
