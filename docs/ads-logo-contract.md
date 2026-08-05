# 📢 Ad Logo (صورة الإعلان) — API Contract for Backend & Mobile Teams

> **Purpose:** The clinic owner can upload a preferred **logo** for their ad from the clinic dashboard (`/Clinic/Marketing`). The backend must store it on the ad and expose it via `imageUrl` so the **patient mobile app** renders it inside the ad.
> This document defines the exact contract the frontend sends/expects. **Frontend side is implemented** — this is the spec for the backend + mobile app.

---

## 1. End-to-End Flow

```
Clinic owner (web dashboard)
   │
   │ 1. Upload logo  ──►  POST {base}/attachments/upload?place=5   → fileName (relative path)
   │
   │ 2. Create ad order (buy)  ──►  POST {base}/clinics/{clinicId}/ads/orders
   │      body includes: { adPackageId, durationDays, logoImageUrl: "<fileName>" }
   │
   ▼
Backend
   │ 3. Creates ad (Status = 0) with LogoImageUrl stored on the ad record
   │
   │ 4. Payment confirmed (Paymob webhook OR admin cash payment type=2)
   │      → Status 0 → 1 (Active), ad keeps LogoImageUrl
   │
   ▼
Patient mobile app
   │ 5. GET {base}/public/ads/active   →  ads[] with imageUrl
   │ 6. Renders imageUrl as the ad visual; falls back to text badge when null
```

---

## 2. Step 1 — Upload the logo (existing endpoint, NO changes needed)

| Item | Value |
|---|---|
| **Method / URL** | `POST {base}/attachments/upload?place=5` |
| **place** | `5` = Clinic/Images (reuse existing clinic image place) |
| **Body** | `multipart/form-data` — field name **`file`**, `accept="image/*"` |
| **Auth** | Clinic owner bearer token |

### Success response (contract)
```json
{
  "success": true,
  "fileName": "clinic-logo-8f3a.png",
  "url": "clinic-logo-8f3a.png"
}
```
> `fileName` / `url` = **relative** path. Full URL is always `{base}/files/{fileName}` — the backend serves attachments at that path.

### Failure response
```json
{ "success": false, "error": "سبب الفشل بالعربية" }
```

---

## 3. Step 2 — Create ad order with logo (backend CHANGE required)

| Item | Value |
|---|---|
| **Method / URL** | `POST {base}/clinics/{clinicId}/ads/orders` |
| **Auth** | Clinic owner bearer token |
| **Content-Type** | `application/json` |

### Request body (what the frontend now sends)
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
| `durationDays` | int | required, multiple of package duration |
| `logoImageUrl` | string \| **null** | **optional** — relative path returned by Step 1. `null` when the clinic didn't upload a logo. |
| `returnUrl` | string \| null | optional; default is now `/Clinic/AdPaymentResult` (in-dashboard result page) |

### Backend requirements
- **MUST accept** `logoImageUrl` (do not reject when absent → treat as null).
- **MUST store** it on the ad record (`Ad.LogoImageUrl` or equivalent) — survives status flips (0 → 1), refunds, and deactivation.
- Same field applies to the **superadmin-created order** (`POST {base}/admin/ads/orders` — `CreateAdsOrderRequest` also carries `logoImageUrl`; the frontend admin modal sends it when present).
- The **cash payment path** (manual payment type=2) activates the clinic's latest pending ad — the logo is already on the ad, nothing extra needed.

---

## 4. Step 3 — Ads responses must include `imageUrl` (backend CHANGE required)

`AdDto` now has `imageUrl` (camelCase). **All** ad endpoints must return it:

| Endpoint | Used by |
|---|---|
| `GET {base}/clinics/{clinicId}/ads` | Clinic dashboard table + refresh |
| `POST {base}/clinics/{clinicId}/ads/orders` (response) | Order confirmation |
| `GET {base}/admin/ads?PageNumber=&PageSize=&Status=` | Superadmin list + preview modal |
| `GET {base}/admin/ads/eligible-clinics` | Admin payments ads modal |
| **`GET {base}/public/ads/active`** | **Patient mobile app** |

### Ad object shape (all endpoints)
```json
{
  "id": "5f2c0000-0000-0000-0000-000000000002",
  "clinicId": "2d1a0000-0000-0000-0000-000000000003",
  "clinicName": "مركز القلب التخصصي",
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
}
```

| Field | Type | Notes |
|---|---|---|
| `imageUrl` | string \| **null** | relative path → full URL = `{base}/files/{imageUrl}`. **`null` when the clinic uploaded no logo.** |

> ⚠️ `imageUrl` must be **`null`** (not missing / not empty-string) when there's no logo — the mobile app checks for null.

---

## 5. Mobile App Rendering Contract

`GET {base}/public/ads/active` returns **only** `status == 1` ads (active). For each ad the app must:

```
IF ad.imageUrl != null:
    render <Image src="{base}/files/{imageUrl}" /> as the ad visual (logo)
    fallback label: ad.clinicName

ELSE (imageUrl == null):
    render text badge:
      • Headline:   ad.clinicName
      • Sub-label:  ad.packageNameAr
      • Period:     ad.startDate → ad.endDate (YYYY-MM-DD)
```

Reference look (matches the web dashboard preview "معاينة الإعلان في تطبيق المرضى"): rounded-square badge — logo image (or clinic initial) on the right, name + package + period on the left, "نشط" tag.

---

## 6. Backend Checklist — what's missing

- [ ] **1. Accept `logoImageUrl`** on `POST /clinics/{clinicId}/ads/orders` (+ admin `POST /admin/ads/orders`), stored on the ad record. Missing field → treat as `null`, **never** 400.
- [ ] **2. Return `imageUrl`** in every ad DTO response — especially `GET /public/ads/active`.
- [ ] **3. Relative path handling** — store the bare fileName (e.g. `clinic-logo-8f3a.png`); full URL composed by consumers as `{base}/files/{fileName}`.
- [ ] **4. Persist across lifecycle** — logo survives Paymob webhook activation, manual cash payment, refund, deactivation.
- [ ] **5. `imageUrl: null` (not missing)** when no logo — a JSON response missing the key entirely is treated as null by the frontend, but mobile SDKs (e.g. Kotlin serialization) may fail — always emit the key.

---

## 7. Frontend Files (already implemented — reference)

| File | What it does |
|---|---|
| `ClinicHub/Views/Clinic/Marketing.cshtml` | Logo upload UI in buy modal (`/Home/UploadAttachment?place=5`), sends `logoImageUrl` in order body, renders `imageUrl` in previews |
| `ClinicHub/Views/Admin/Ads.cshtml` | Preview modal renders `imageUrl` |
| `ClinicHub.Services/ReponseModels/AdDto.cs` | `ImageUrl` field added |
| `ClinicHub.Services/RequestModels/CreateAdsOrderRequest.cs` | `LogoImageUrl` field added |
