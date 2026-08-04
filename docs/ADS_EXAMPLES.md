# 📢 Ads Plans & Subscription — Worked Examples (Readme)

> Companion doc to `docs/ADS_MANAGEMENT.md`.
> This file shows **concrete, runnable examples** with real JSON payloads, sample data, and the exact code paths that handle each step — from the clinic subscribing to an advanced plan, to the patient app rendering the ad.

---

## 0. TL;DR — The whole flow in one picture

```
ADVANCED PLAN (has "marketing_tools")
        │
        ▼
Clinic subscribes ──POST /subscriptions/initiate-payment──► Paymob ──► SubscriptionDto (PlanFeatures += MarketingTools)
        │
        ▼
Clinic sees "الخدمات الإعلانية" page (/Clinic/Marketing)
        │
        ├─► Buys ad: POST /clinics/{clinicId}/ads/orders ──► Paymob ──► AdDto Status 0
        │                                                              │ (poll /Clinic/GetMyAdsJson)
        │                                                              ▼
        │                                                         Status 1 (نشط)
        │
        └─► (Alt) Super Admin sells it: POST /admin/ads/orders  OR  cash: POST /admin/payments/manual (type=2)
        │
        ▼
GET /public/ads/active  (only Status==1)  ──►  PATIENT MOBILE APP renders as image/badge
```

---

## 1. Sample Data (mock-style, using the frontend DTOs)

### 1.1 Plans (from `PlanDto`)

| Field | Basic | **Advanced** ✅ | Premium |
|---|---|---|---|
| `id` | `1111…` | `3f9a1c22-aaaa-4aaa-8aaa-000000000001` | `3f9a1c22-aaaa-4aaa-8aaa-000000000002` |
| `nameAr` | الأساسية | **المتقدمة** | الممتازة |
| `priceMonthly` | 700 | **1500** | 3000 |
| `priceYearly` | 7000 | **15000** | 30000 |
| `maxDoctors` | 2 | 5 | 10 |
| `maxStaff` | 3 | 10 | 25 |
| `features` | `["appointments","patient_records","basic_reports","online_booking"]` | `["appointments","patient_records","advanced_reports","marketing_tools","online_booking","staff_management","doctor_management"]` | `[...,"marketing_tools",...,"priority_support"]` |
| **Ads?** | ❌ No | ✅ **Yes** | ✅ Yes |

> `"marketing_tools"` is the only key that unlocks ads (`PlanFeatureMap` in `Data/Roles.cs:79`).

### 1.2 Ad packages (from `AdPackageDto`, managed by Super Admin)

| `id` | `nameAr` | `price` | `durationDays` | `isActive` |
|---|---|---|---|---|
| `adpkg-8001` | لافتة أسبوع (Banner 7d) | 300 | 7 | true |
| `adpkg-8002` | لافتة شهرية (Banner 30d) | 1000 | 30 | true |
| `adpkg-8003` | شارة مميزة (Badge 90d) | 2500 | 90 | true |

### 1.3 Clinic

```json
{ "id": "5f2c0000-0000-0000-0000-000000000001", "name": "عيادة القلب الحديثة" }
```

---

## 2. Example A — Clinic subscribes to the Advanced plan (gets ads right)

**Request** — `ClinicController.Subscribe` → `SubscriptionService.InitiatePaymentAsync`
```http
POST {base}/subscriptions/initiate-payment
Content-Type: application/json
```
```json
{
  "planId": "3f9a1c22-aaaa-4aaa-8aaa-000000000001",
  "period": 0,
  "returnUrl": "https://app.doctory.test/Home/PaymentResult"
}
```

**Backend reply** → `InitiatePaymentResponseDto` (Paymob redirect URL + payment key).

**After payment** — `ClinicController.OnActionExecutionAsync` (`ClinicController.cs:77-89`) builds:
```csharp
PlanFeatures = PlanFeatureMap.FromFeatureStrings([... "marketing_tools" ...]),  // => MarketingTools bit set
HasActivePlan = !isExpired
```
→ Sidebar now shows **"الخدمات الإعلانية"** (`_ClinicLayout.cshtml:122`).

**Handling rule enforced:** without `marketing_tools` in the subscription's plan, `_ClinicLayout` hides the link and `Marketing.cshtml` shows the **upsell card** instead of the buy flow.

---

## 3. Example B — Clinic buys an ad themselves

**Request** — `ClinicController.CreateAdOrder` → `AdService.CreateOrderAsync`
```http
POST {base}/clinics/5f2c0000-0000-0000-0000-000000000001/ads/orders
```
```json
{
  "clinicId": "5f2c0000-0000-0000-0000-000000000001",
  "adPackageId": "adpkg-8001",
  "durationDays": 14,
  "returnUrl": "https://app.doctory.test/Home/PaymentResult?type=ads"
}
```
> Duration is a multiple of the package base (7×2 = 14 days). Price = 300 × (14/7) = **600 ج.م**.

**Backend reply** → `AdsOrderResponseDto`
```json
{
  "paymentId": "pay-9001",
  "refNumber": "AD-2026-0001",
  "amount": 600.00,
  "currency": "EGP",
  "status": 0,
  "paymobRedirectUrl": "https://accept.paymob.com/api/acceptance/iframes/...",
  "paymobPaymentKey": "pk_..."
}
```

**Frontend handling** (`Marketing.cshtml` JS):
1. `window.open(paymobRedirectUrl)` in a new tab.
2. `pollAdActivation()` calls `POST /Clinic/GetMyAdsJson` every 2s (max 5 tries).
3. When `status` becomes `1`, shows success modal + reload.

**Lifecycle of that `AdDto`:**
| Field | value |
|---|---|
| `id` | `ad-5001` |
| `clinicName` | عيادة القلب الحديثة |
| `packageNameAr` | لافتة أسبوع |
| `durationDays` | 14 |
| `amount` | 600.00 |
| `status` | `0` → `1` |
| `startDate` | 2026-08-01 |
| `endDate` | 2026-08-15 |

---

## 4. Example C — Super Admin sells / manages an ad

### C1. Sell an ad on a clinic's behalf (Paymob)
`AdminController.CreateAdsOrder` → `AdminPaymentService.CreateAdsOrderAsync`
```http
POST {base}/admin/ads/orders
```
```json
{ "clinicId": "5f2c0000-...-0001", "adPackageId": "adpkg-8002", "durationDays": 30 }
```
Same `AdsOrderResponseDto` return shape → the modal in `Admin/Payments.cshtml` opens Paymob.

### C2. Record a cash payment (activate offline, no Paymob)
`AdminController.CreateManualPayment` → `AdminPaymentService.CreateManualPaymentAsync`
```http
POST {base}/admin/payments/manual
```
```json
{
  "payerId": "5f2c0000-...-0001",
  "type": 2,            // 2 = خدمة إعلانية (see Payments.cshtml:113)
  "amount": 1000.00,
  "method": 0,          // 0 = نقدي
  "notes": "دفعة نقدية لإعلان ad-5002"
}
```
Button lives in `Admin/Ads.cshtml` (`recordCashPayment`).

### C3. Deactivate an ad (with reason)
`AdminController.DeactivateAd` → `AdService.DeactivateAdAsync`
```http
POST {base}/admin/ads/ad-5001/deactivate
```
```json
{ "reason": "محتوى مخالف — صورة غير مطابقة للاختصاص" }
```
→ `AdDto.status` = `3` (ملغي), removed from patient feed.

### C4. Packages CRUD
- Create: `POST {base}/admin/ads/packages` — `UpsertAdPackageRequest` (see section 1.2)
- Update / toggle: `PUT {base}/admin/ads/packages/{id}`
- Delete: `DELETE {base}/admin/ads/packages/{id}`

---

## 5. Example D — Patient mobile app shows the ad

**Consumed endpoint:** `GET {base}/public/ads/active` (`DoctoryRoutes.Ads.PublicActive`)

Returns only ads where `status == 1`. The patient app renders each as an **image or badge**:

```
┌──────────────────────────┐
│  ◉ عيادة القلب الحديثة   │  ← badge (logo / first letter + name)
│     لافتة أسبوع • 14 يوم │
│     2026-08-01 → 08-15   │
└──────────────────────────┘
```

The web dashboard mirrors this exact layout via the `.ad-preview-*` block:
- **Clinic owner** sees "معاينة الإعلان في تطبيق المرضى" on `/Clinic/Marketing`.
- **Super Admin** sees it via the 👁 button in `/Admin/Ads`.

---

## 6. Handling rules cheat-sheet

| Concern | Rule | Enforced at |
|---|---|---|
| Ads only for advanced plans | plan `features` must contain `"marketing_tools"` | `Data/Roles.cs` + `_ClinicLayout` + `Marketing.cshtml:9` |
| Ad must be paid before public | `status == 1` required | backend `/public/ads/active` |
| Duration = package base × integer | 1..12 multiplier | `Marketing.cshtml` + `Payments.cshtml` JS |
| Cash can activate without Paymob | manual payment `type=2, method=0` | `Admin/Ads.cshtml` |
| Super Admin is the referee | deactivate + reason, review all clinics | `Admin/Ads.cshtml` + `AdminController.DeactivateAd` |
| Patient only sees active | `status==0,2,3` hidden | backend public feed |

---

## 7. Gotcha fixes already applied (integration layer)

| Issue | Fix |
|---|---|
| `Error converting value {null} to type 'System.Boolean'` on verification approve/reject | `UserVerificationService` now checks `IsSuccessStatusCode` + parses via `JObject` (`ParseBoolResponse`) instead of strict `ApiResponse<bool>` deserialization |

---

## 8. Try it locally (checklist)

1. Super Admin → `Admin/Ads` → **إضافة باقة إعلانية** — create `adpkg-8001`.
2. Clinic owner → subscribe to Advanced plan → open `/Clinic/Marketing`.
3. Click **شراء إعلان جديد** → pick package + 14 days → confirm price = 600 ج.م → pay (goes to `/Home/PaymentResult?type=ads`).
4. Watch status flip `0 → 1` in the "إعلاناتي" table; active ad appears in the **patient-app preview** card.
5. (Optional) Super Admin → Payments → **طلب خدمة إعلانية** for another clinic, or record cash payment for a pending ad.
6. Super Admin → `Admin/Ads` → 👁 on an active ad row → see the exact patient-app badge.