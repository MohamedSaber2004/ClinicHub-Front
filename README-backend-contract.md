# Backend API Contract — Frontend Expectations

This document outlines exactly what the frontend expects from backend API endpoints.  
If any of these contracts are violated, features will silently break (sidebar items disappear, null references, etc.).

---

## 1. `GET /api/v1/plans` (public)

### Expected Response Format

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Basic",
    "nameAr": "أساسية",
    "description": "string or null",
    "descriptionAr": "string or null",
    "priceMonthly": 500.00,
    "priceYearly": 5000.00,
    "maxDoctors": 2,
    "maxStaff": 5,
    "features": "[\"appointments\",\"patient_records\",\"basic_reports\",\"online_booking\",\"staff_management\",\"doctor_management\"]",
    "isActive": true,
    "sortOrder": 1
  }
]
```

### Critical Rules

| Rule | Why |
|------|-----|
| **Must return a JSON array `[...]`** (NOT wrapped in `{data: [...]}`) | Frontend parser falls back to direct array parse; `JObject` parse throws on arrays |
| **Property names must be camelCase** (`id`, `name`, `nameAr`, `features`, `isActive`...) | The C# deserializer uses `CamelCasePropertyNamesContractResolver` |
| **`features` must be a JSON string** containing a JSON array, e.g. `"[\"appointments\"]"` (NOT a native JSON array `["appointments"]`) | `PlanDto.Features` is `string` type, not `List<string>` |
| **`features` array items MUST match these exact keys** | See feature key table below |
| **`id` must be a valid GUID string** | Compared with `subscription.planId` to find the user's plan |
| **Each plan MUST have a unique `id`** | Used to match user's subscription to plan features |
| **`isActive` must be `true` for available plans** | Plans are filtered client-side by `isActive` |

### Feature Keys That Must Appear in `features`

| Key | PlanFeature | Basic Plan | Advanced Plan |
|-----|------------|:----------:|:-------------:|
| `appointments` | `ManageAppointments` | ✅ | ✅ |
| `patient_records` | `ManagePatientRecords` | ✅ | ✅ |
| `basic_reports` | `BasicReports` | ✅ | ✅ |
| `online_booking` | `OnlineBooking` | ✅ | ✅ |
| `staff_management` | `ManageStaff` | ✅ | ✅ |
| `doctor_management` | `ManageDoctors` | ✅ | ✅ |
| `advanced_reports` | `AdvancedReports` | — | ✅ |
| `marketing_tools` | `MarketingTools` | — | ✅ |
| `priority_support` | `PrioritySupport` | — | ✅ |

> **Do NOT add `sms_notifications`** — the frontend explicitly ignores it per `README-frontend-integration.md`. Adding it will have no effect.

---

## 2. `GET /api/v1/subscriptions/my` (auth required)

### Expected Response Format

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clinicName": "عيادة السلام الطبي",
  "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "planName": "متقدمة",
  "period": 0,
  "startDate": "2026-07-23T00:00:00",
  "endDate": "2027-07-23T00:00:00",
  "status": 0,
  "amount": 15000.00,
  "paidAt": "2026-07-23T00:00:00",
  "isActive": true
}
```

### Critical Rules

| Rule | Why |
|------|-----|
| **Must return a JSON object `{...}`** (NOT wrapped in `{data: {...}}`) | Frontend tries both; bare object works directly |
| **Property names must be camelCase** (`planId`, `isActive`, `clinicName`...) | Same CamelCase deserializer |
| **`planId` must match a plan's `id` from `/api/v1/plans`** | Used to find the user's plan and resolve features |
| **`isActive` must be `true` for current subscriptions** | Controls `HasActivePlan` flag in sidebar |
| **`startDate` / `endDate` must be ISO 8601 strings** | `SubscriptionDto` uses `DateTime` type |
| **Do NOT include `permissions` array** (or include but frontend ignores it) | Feature resolution uses `plan.features`, not subscription permissions |

---

## 3. `POST /api/v1/subscriptions/initiate-payment` (auth required)

### Expected Request Format (sent by frontend)

```json
{
  "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "period": 0,
  "returnUrl": "https://clinic-hub.example.com/Home/PaymentResult"
}
```

### Expected Response Format

```json
{
  "paymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "paymobRedirectUrl": "https://paymob.com/...",
  "redirectUrl": "https://...",
  "paymentUrl": "https://...",
  "url": "https://...",
  "paymobPaymentKey": "string",
  "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "planName": "متقدمة",
  "period": 0,
  "amount": 15000.00,
  "currency": "EGP"
}
```

### Critical Rules

| Rule | Why |
|------|-----|
| `paymobRedirectUrl` / `redirectUrl` / `paymentUrl` / `url` — **at least one must be non-empty** | Used to redirect the user to payment; `TargetRedirectUrl` picks the first non-empty in priority order |
| **Return `200` on success** (not `201`, not `302`) | Frontend checks `IsSuccessStatusCode` |
| On failure, return **non-2xx with error message in standard format** | Error is displayed to user via `TempData["ErrorMessage"]` |

---

## 4. Common Rules for ALL Endpoints

| Rule | Impact |
|------|--------|
| **Always return camelCase JSON** (`planId`, not `PlanId`) | `CamelCasePropertyNamesContractResolver` on all DTOs |
| **Never return empty body on 200** | Frontend checks `string.IsNullOrWhiteSpace(responseBody)` and throws `ApiException` |
| **Never return HTML on API endpoints** | JSON parse will throw → fallback to empty/invalid data |
| **Do NOT wrap responses in `{data: ...}`** | Frontend handles both, but direct access is preferred and more reliable |
| **Status codes**: 200 for success, 4xx for client errors, 5xx for server errors | Frontend throws `ApiException` with status code and message |

### What Happens When the Contract is Violated

| Violation | Symptom |
|-----------|---------|
| `features` has wrong keys | Those PlanFeature flags are never set → sidebar items stay hidden |
| `planId` doesn't match any plan's `id` | `plan` is null → `PlanFeatures = None` → ALL plan-gated items hidden |
| Wrong casing (PascalCase instead of camelCase) | All `PlanDto`/`SubscriptionDto` properties get default values (null, 0, Guid.Empty) |
| Empty response body on 200 | `ApiException(500, "استجابة فارغة من الخادم")` — shown as error to user |
| Plans endpoint down or non-2xx | Caught by `catch` block → `HasActivePlan = false` → sidebar limits all plan features |
| `features` is a native JSON array `["app"]` instead of string `"[\"app\"]"` | `System.Text.Json` deserialization of string as array fails → empty feature list |