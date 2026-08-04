# 📢 Ads Management Feature — Implementation Analysis

> **Scope:** Frontend (ASP.NET Core MVC) + API integration layer for the **Doctory / ClinicHub** platform.
> This document describes **what exists today**, how the three business actors interact with ads, and the **frontend adaptations** made to match the preferred business model.

---

## 1. Business Model (Preferred)

The ads feature serves **three actors**:

```
┌─────────────────────────┐        ┌──────────────────────────┐
│  1. Clinic Owner        │        │  2. Super Admin          │
│  (Web dashboard)        │        │  (Web dashboard)         │
│                         │        │                          │
│  • Subscribes to an     │        │  • Manages ad packages   │
│    ADVANCED plan that   │        │  • Reviews all clinic    │
│    includes "marketing  │        │    ads (any clinic)      │
│    tools" feature       │        │  • Deactivates ads w/    │
│  • Buys ad packages     │        │    reason                │
│    (Paymob payment)     │        │  • Records cash payments │
│  • Tracks own ads       │        │  • Creates ad orders on  │
│                         │        │    behalf of clinics     │
└────────────┬────────────┘        └────────────┬─────────────┘
             │                                  │
             ▼                                  ▼
   ┌───────────────────────────────────────────────────┐
   │              Ads Database (Backend API)           │
   │   GET /public/ads/active  →  active ads only      │
   └───────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────┐
│  3. Patient (Mobile App — EXTERNAL, not in repo)    │
│                                                     │
│  • Sees clinic ads as IMAGES / BADGES               │
│    (banner image or badge chip with clinic name)    │
│  • Consumes public API: /public/ads/active          │
└─────────────────────────────────────────────────────┘
```

### Core Rules
| Rule | Where enforced |
|---|---|
| Ads are **only** available on plans with `marketing_tools` feature (advanced plans) | `_ClinicLayout.cshtml` sidebar, `Marketing.cshtml` (`canBuy`) |
| Ad becomes visible in patient app **only after payment confirmed** (status = 1) | Backend `public/ads/active` returns only active ads |
| Super Admin manages ads of **all clinics**, not just one | `Admin/Ads` page |
| Ad duration = package base duration × integer multiplier (1..12) | `Marketing.cshtml`, `Payments.cshtml` JS |
| Cash payments can activate an ad without Paymob | `Admin/Ads.cshtml` → `recordCashPayment` |

---

## 2. Current Implementation Map (Files)

### 2.1 Services Layer (`ClinicHub.Services/`)

| File | Role |
|---|---|
| `Contracts/IAdService.cs` | Contract: clinic-side + admin-side ad operations |
| `Services/Implementations/AdService.cs` | HTTP calls to backend API |
| `Services/Implementations/AdminPaymentService.cs` | Ads order creation (admin), eligible clinics, ad packages, manual payments |
| `ReponseModels/AdDto.cs` | Ad DTO: `Id, ClinicId, ClinicName, PackageId, PackageNameAr, DurationDays, Amount, Currency, Status, StartDate, EndDate, CreatedAt` |
| `ReponseModels/AdPackageDto.cs` | Ad package DTO: `Id, Name, NameAr, Description, DescriptionAr, Price, DurationDays, IsActive` |
| `ReponseModels/AdsOrderResponseDto.cs` | Order result: `PaymentId, RefNumber, Amount, Currency, Status, PaymobRedirectUrl, PaymobPaymentKey` |
| `ReponseModels/EligibleClinicDto.cs` | Clinics eligible for ads (used in admin payments modal) |
| `RequestModels/CreateAdsOrderRequest.cs` | `ClinicId, AdPackageId, DurationDays, ReturnUrl` |
| `RequestModels/UpsertAdPackageRequest.cs` | Package create/update payload |
| `Routes/Api/DoctoryRoutes.cs` | All API route builders (section 4) |

### 2.2 Controllers (`ClinicHub/Controllers/`)

**ClinicController.cs** (clinic owner)
| Action | Route | Purpose |
|---|---|---|
| `Marketing()` | `/Clinic/Marketing` | Loads `ViewBag.Ads`, `ViewBag.Packages`; renders marketing page |
| `CreateAdOrder([FromBody])` | `POST /Clinic/CreateAdOrder` | Creates ad order via `_adService.CreateOrderAsync`; sets `ReturnUrl=/Home/PaymentResult?type=ads` |
| `GetMyAdsJson()` | `POST /Clinic/GetMyAdsJson` | AJAX refresh of clinic's ads (used by activation poller) |

**AdminController.cs** (super admin)
| Action | Route | Purpose |
|---|---|---|
| `Ads(pageNumber, pageSize, status)` | `/Admin/Ads` | List + filter ads; loads packages for tab |
| `CreateAdsOrder([FromBody])` | `POST /Admin/CreateAdsOrder` | Super admin creates ad order **on behalf of a clinic** (from Payments page) |
| `DeactivateAd(id, body)` | `POST /Admin/DeactivateAd?id=` | Cancel ad with optional reason |
| `CreateAdPackage([FromBody])` | `POST /Admin/CreateAdPackage` | Create package (validates price/duration > 0) |
| `UpdateAdPackage(id, [FromBody])` | `POST /Admin/UpdateAdPackage?id=` | Update/toggle package |
| `DeleteAdPackage(id)` | `POST /Admin/DeleteAdPackage?id=` | Delete package |
| `Payments(...)` | `/Admin/Payments` | Loads `ViewBag.EligibleClinics`, `ViewBag.AdPackages` for the ads-order modal + ads revenue stat |

### 2.3 Views (`ClinicHub/Views/`)

| View | Role |
|---|---|
| `Clinic/Marketing.cshtml` | **Clinic owner** ads page: package cards, "my ads" table, buy modal (Paymob), activation poller. **NEW:** patient-app badge preview (adaptation) |
| `Admin/Ads.cshtml` | **Super admin** ads page: tabs (Ads / Packages), status filter, deactivate modal with reason, cash-payment action, package CRUD modals. **NEW:** preview modal + hint (adaptation) |
| `Admin/Payments.cshtml` | Payments page with **"طلب خدمة إعلانية"** modal (eligible clinics + packages + Paymob) and manual payment modal |
| `Home/PaymentResult.cshtml` | Paymob redirect landing — ads-aware (`type=ads`): success/failure messaging + countdown redirect to `/Clinic/Marketing` |
| `Home/Subscriptions.cshtml` | Public plan catalog — renders `marketing_tools` as "أدوات تسويقية" |
| `Admin/Subscriptions.cshtml` | Admin plan catalog (same label map) |
| `Shared/_ClinicLayout.cshtml` | Sidebar: shows "الخدمات الإعلانية" only if `HasFeature(MarketingTools) && HasActivePlan` |
| `Shared/_AdminLayout.cshtml` | Sidebar: "إدارة الإعلانات" link |
| `Clinic/Index.cshtml` | Dashboard plan-info bar shows "أدوات تسويقية" badge when feature active |

### 2.4 CSS

| File | Contents |
|---|---|
| `wwwroot/css/design-system.css` | Design tokens (`--clr-*`, `--space-*`, `--fs-*`, `--radius-*`) |
| `wwwroot/css/site.css` | `/* Ads */` section (~line 1122): `.upsell-card`, `.upsell-icon`, `.upsell-body`, `.upsell-title`, `.upsell-text`, `.packages-row`, `.package-mini-card`, `.package-mini-name`, `.package-mini-desc`, `.package-mini-price`, `.form-hint`. **NEW:** `.ad-preview-*` classes (adaptation) |

### 2.5 Feature Gating (plan → feature)

- `Data/Roles.cs` defines `PlanFeature.MarketingTools = 1L << 4` and maps JSON feature key `"marketing_tools"`.
- `Data/CurrentUserContext.cs` exposes `HasFeature(PlanFeature)` + `HasActivePlan`.
- `ClinicController.OnActionExecutionAsync` builds `CurrentUser` from the subscription's plan feature list every request.
- The "advanced plan" is any plan whose `Features` JSON array includes `"marketing_tools"`.

---

## 3. Lifecycle of an Ad

```
1. Clinic owner subscribed to advanced plan (marketing_tools)
2. Opens /Clinic/Marketing → sees packages (GET /ads/packages)
3. Clicks "شراء إعلان جديد" → picks package + duration multiplier
4. POST /Clinic/CreateAdOrder
        └─ backend creates AdsOrder with payment intent (Paymob)
        └─ returns PaymobRedirectUrl
5. Owner pays on Paymob (new tab)
6. Paymob redirects to /Home/PaymentResult?type=ads
7. Marketing page polls POST /Clinic/GetMyAdsJson (5 × 2s)
        └─ when ad Status 0 → 1, shows success modal + reload
8. Backend makes ad visible via GET /public/ads/active
        └─ PATIENT MOBILE APP shows it as image/badge
```

**Alternative (offline) path:** Super Admin records a cash payment (`POST /Admin/CreateManualPayment`, `type=2`, `method=0`) → ad activates immediately without Paymob.

---

## 4. Backend API Endpoints Consumed

| Endpoint (builder) | Method | Used by |
|---|---|---|
| `GET {base}/clinics/{clinicId}/ads?Status=` | `Ads.MyAds` | Clinic owner: my ads |
| `POST {base}/clinics/{clinicId}/ads/orders` | `Ads.CreateOrder` | Clinic owner: buy ad |
| `GET {base}/ads/packages` | `Ads.Packages` | Clinic owner: active packages |
| `GET {base}/public/ads/active` | `Ads.PublicActive` | **Patient mobile app** (external) |
| `GET {base}/admin/ads?PageNumber=&PageSize=&Status=` | `AdminAds.List` | Super admin list |
| `GET {base}/admin/ads/eligible-clinics` | `AdminAds.EligibleClinics` | Admin payments modal |
| `GET/POST {base}/admin/ads/packages` | `AdminAds.Packages` | Super admin packages CRUD |
| `PUT/DELETE {base}/admin/ads/packages/{id}` | `AdminAds.Package` | Super admin package update/delete |
| `POST {base}/admin/ads/orders` | `AdminAds.Orders` | Super admin creates order for clinic |
| `POST {base}/admin/ads/{id}/deactivate` | `AdminAds.Deactivate` | Super admin cancels ad |

---

## 5. Ad Status Codes (shared by both views)

| Code | Meaning | Badge class |
|---|---|---|
| `0` | Pending payment (معلق الدفع) | `badge-warning` |
| `1` | Active (نشط) | `badge-success` |
| `2` | Expired (منتهي) | `badge-info` |
| `3` | Cancelled (ملغي) | `badge-danger` |

> `0` ads also show the "تسجيل دفعة نقدية" action for the super admin.

---

## 6. What is NOT in this repo (patient mobile app)

- The **patient mobile application** is external. It consumes `GET /public/ads/active` and renders ads as **images/badges**.
- No patient-facing ad page exists in this web frontend (by design — patients use the app).
- **Important:** the current `AdDto` contains **no image field** — ad *visual content* (image/badge asset) is served by the backend/patient app layer. The frontend displays ad **metadata** (clinic name, package, period, status).

---

## 7. Frontend Adaptations Applied (to match preferred business)

To make the three-actor model explicit and consistent in the web frontend, the following were added:

### 7.1 `Clinic/Marketing.cshtml` — patient-app preview
- New section **"معاينة الإعلان في تطبيق المرضى"**.
- Active ads (`Status == 1`) render as **badge cards** (image-style block with clinic name, package, period) — the exact shape patients see in the mobile app.
- Empty state when no active ads → guides owner to buy an ad.

### 7.2 `Admin/Ads.cshtml` — preview modal + context hint
- New **"معاينة"** (eye) button per active ad row.
- Opens a modal rendering the ad as it appears in the patient app (badge card).
- Hint text under the filter bar explains the super admin's role: "المرضى يشاهدون الإعلانات النشطة كصور/شارات في تطبيق الجوال".

### 7.3 `site.css` — `/* Ads Preview */` block
- `.ad-preview-card`, `.ad-preview-grid`, `.ad-preview-item`, `.ad-preview-visual`, `.ad-preview-visual--lg`, `.ad-preview-body`, `.ad-preview-name`, `.ad-preview-meta`, `.ad-preview-period`, `.ad-preview-empty`, `.ad-preview-hint` — all built on design-system tokens.

### 7.4 No changes needed (already aligned)
- Plan gating (`marketing_tools` → advanced plan) ✅
- Admin ad management surface ✅
- Paymob order flow + `PaymentResult?type=ads` ✅
- Sidebar visibility rules ✅

---

## 8. Consistency Notes & Possible Future Work

1. **`--fs-sm` used but undefined** in `design-system.css` (referenced at line 859, no token defined). Consider adding `--fs-sm: 12px` to tokens.
2. **Ad image/badge asset**: if clinics should upload a custom ad image, a new `ImageUrl` field + attachment upload endpoint would be needed in the backend, then surfaced in `AdDto` and both previews.
3. **Public web preview of ads** could be exposed as a public page (e.g., `/Ads/Public`) reusing `Ads.PublicActive` — currently only the mobile app uses it.
4. **Package sort/feature metadata** (e.g., "banner vs badge" placement type) is not modeled yet; `AdPackageDto` has no placement/type field.

---

## 9. Quick Reference (routes)

| Page | Route |
|---|---|
| Clinic owner ads | `/Clinic/Marketing` |
| Clinic buy order (AJAX) | `POST /Clinic/CreateAdOrder` |
| Clinic ads refresh (AJAX) | `POST /Clinic/GetMyAdsJson` |
| Super admin ads | `/Admin/Ads` |
| Super admin deactivate | `POST /Admin/DeactivateAd?id=` |
| Super admin packages CRUD | `/Admin/CreateAdPackage`, `/Admin/UpdateAdPackage?id=`, `/Admin/DeleteAdPackage?id=` |
| Admin order for clinic | `POST /Admin/CreateAdsOrder` |
| Admin manual payment | `POST /Admin/CreateManualPayment` (type 2 = ads) |
| Payment landing | `/Home/PaymentResult?type=ads` |
| Public plan catalog | `/Home/Subscriptions` |
