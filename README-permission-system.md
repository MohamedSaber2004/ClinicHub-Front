# خطة تكامل صلاحيات الباقات (Subscription Permission System)

## Overview

The frontend enforces **plan-based access control** by reading the plan's feature list
(from the subscription/plan API) and mapping each feature string to a `PlanFeature` flag.
Views and sidebar items check `CurrentUserContext.HasFeature(PlanFeature.X)` before
rendering.

## Backend → Frontend Contract

### 1. Plans API (`GET /api/v1/plans`)

Each plan must include a `features` field — a JSON array of **feature key strings**.
These keys are the single source of truth; the frontend maps them to internal flags.

```json
[
  {
    "id": "guid",
    "name": "Basic",
    "nameAr": "أساسية",
    "priceMonthly": 500,
    "priceYearly": 5000,
    "maxDoctors": 2,
    "maxStaff": 5,
    "features": "[\"appointments\",\"patient_records\",\"basic_reports\",\"online_booking\",\"staff_management\",\"doctor_management\"]",
    "isActive": true,
    "sortOrder": 1
  },
  {
    "id": "guid",
    "name": "Standard",
    "nameAr": "قياسية",
    "priceMonthly": 1000,
    "priceYearly": 10000,
    "maxDoctors": 5,
    "maxStaff": 15,
    "features": "[\"appointments\",\"patient_records\",\"basic_reports\",\"advanced_reports\",\"sms_notifications\",\"online_booking\",\"staff_management\",\"doctor_management\"]",
    "isActive": true,
    "sortOrder": 2
  },
  {
    "id": "guid",
    "name": "Premium",
    "nameAr": "بريميوم",
    "priceMonthly": 2000,
    "priceYearly": 20000,
    "maxDoctors": null,
    "maxStaff": null,
    "features": "[\"appointments\",\"patient_records\",\"basic_reports\",\"advanced_reports\",\"sms_notifications\",\"marketing_tools\",\"priority_support\",\"online_booking\",\"staff_management\",\"doctor_management\"]",
    "isActive": true,
    "sortOrder": 3
  }
]
```

**Key rules:**
- `features` is a **JSON string** (not a raw array), deserialized by the frontend.
- `maxDoctors` / `maxStaff` = `null` means **unlimited**.
- The `nameAr` is used for display in the sidebar/clinic dashboard.

### 2. My Subscription API (`GET /api/v1/subscriptions/my`)

Returns the clinic's current subscription including plan info so the frontend can
resolve plan features and limits.

```json
{
  "id": "guid",
  "clinicId": "guid",
  "clinicName": "عيادة السلام الطبي",
  "planId": "guid",
  "planName": "قياسية",
  "period": 0,
  "startDate": "2026-07-23T00:00:00",
  "endDate": "2027-07-23T00:00:00",
  "status": 0,
  "amount": 10000,
  "paidAt": "2026-07-23T00:00:00",
  "isActive": true
}
```

The frontend uses `planId` to look up the plan details (via plans API or embedded) and
reads the `features` + `maxDoctors` + `maxStaff` from it.

### 3. Current User API (`GET /api/v1/auth/me`)

Should eventually include the active plan information so the frontend can set
`CurrentUserContext` correctly after login.

```json
{
  "id": 6,
  "fullName": "...",
  "email": "...",
  "role": "ClinicOwner",
  "permissions": [...],
  "plan": {
    "id": "guid",
    "name": "قياسية",
    "features": ["appointments", "patient_records", ...],
    "maxDoctors": 5,
    "maxStaff": 15,
    "isActive": true
  }
}
```

## Feature Key → PlanFeature Mapping

The frontend (`Roles.cs:PlanFeatureMap`) maintains a hard-coded dictionary that
translates API feature keys to `PlanFeature` flags:

| API Feature Key         | PlanFeature Flag         |
|-------------------------|--------------------------|
| `appointments`          | `ManageAppointments`     |
| `patient_records`       | `ManagePatientRecords`   |
| `basic_reports`         | `BasicReports`           |
| `advanced_reports`      | `AdvancedReports`        |
| `sms_notifications`     | `SmsNotifications`       |
| `marketing_tools`       | `MarketingTools`         |
| `priority_support`      | `PrioritySupport`        |
| `online_booking`        | `OnlineBooking`          |
| `staff_management`      | `ManageStaff`            |
| `doctor_management`     | `ManageDoctors`          |

**If you add a new feature key on the backend, you MUST add a corresponding entry**
**in `PlanFeatureMap.FeatureKeyMap` (in `Data/Roles.cs`).**

## Plan Permission Matrix

| Feature                | Basic | Standard | Premium |
|------------------------|-------|----------|---------|
| إدارة المواعيد         | ✓     | ✓        | ✓       |
| السجلات الطبية         | ✓     | ✓        | ✓       |
| تقارير أساسية          | ✓     | ✓        | ✓       |
| الحجز والدفع أونلاين   | ✓     | ✓        | ✓       |
| إدارة الموظفين         | ✓     | ✓        | ✓       |
| إدارة الأطباء          | ✓     | ✓        | ✓       |
| تقارير متقدمة          | —     | ✓        | ✓       |
| إشعارات SMS            | —     | ✓        | ✓       |
| أدوات تسويقية          | —     | —        | ✓       |
| دعم ذو أولوية          | —     | —        | ✓       |

## What the Frontend Does With This Data

### On page load (`ClinicController.OnActionExecuting`):

1. Fetches subscription from `_subscriptionService.GetMySubscriptionAsync()`
2. Fetches plan list from `_planService.GetAllAsync()`
3. Finds the matching plan by `planId`
4. Deserializes `plan.Features` JSON → `List<string>`
5. Calls `PlanFeatureMap.FromFeatureStrings(features)` → `PlanFeature` flags
6. Sets `CurrentUserContext.PlanFeatures`, `MaxDoctors`, `MaxStaff`, `PlanName`

### In views:

- **Sidebar** (`_ClinicLayout.cshtml`): items check
  `_user.HasFeature(PlanFeature.X)` before rendering. Features not in the plan
  are hidden. Exclusive features like marketing tools and priority support only
  show for plans that include them.
- **Dashboard** (`Clinic/Index.cshtml`): shows a plan-info bar with limits and
  enabled premium features.
- **Staff page** (`Clinic/Staff.cshtml`): shows a limit notice when
  `MaxStaff` has a value.
- **Subscription banner** (`_SubscriptionBanner.cshtml`): shows active plan
  limits or warns when subscription is inactive.

## Backend Responsibilities

### What the backend MUST provide:

| Data                     | Endpoint                             | Used For                            |
|--------------------------|--------------------------------------|--------------------------------------|
| Plans with features list | `GET /api/v1/plans`                  | Feature resolution, limits, pricing  |
| Current subscription     | `GET /api/v1/subscriptions/my`       | Plan resolution, active status       |
| User role + permissions  | `GET /api/v1/auth/me`                | Role-based permission checks         |

### What the backend SHOULD enforce:

- `[RequirePlanPermission]` action filter (or equivalent) on controller actions
  to prevent direct URL access to features not in the user's plan.
- API responses should return `403 Forbidden` with a meaningful message when
  the plan does not include a required feature.

## How to Add a New Plan Feature

1. **Backend:** Add the feature key string to the plan's `features` JSON array
   for plans that should have it.
2. **Frontend:** Add a new entry in `PlanFeature` enum (powers of 2) and a
   matching entry in `PlanFeatureMap.FeatureKeyMap` in `Data/Roles.cs`.
3. **Frontend:** Gate the UI element with
   `_user.HasFeature(PlanFeature.YourNewFeature)` in the relevant view.
4. **Frontend (optional):** Add the feature label in
   `Views/Home/Subscriptions.cshtml` `featureLabels` dictionary for the
   pricing page display.

## Example: Adding "Telemedicine" Feature

**Step 1 — Backend plan features (add key to JSON):**
```
Basic:   "...telemedicine..."
Premium: "...telemedicine..."
```

**Step 2 — Frontend enum + map (`Data/Roles.cs`):**
```csharp
[Flags]
public enum PlanFeature : long
{
    // ... existing ...
    Telemedicine = 1L << 10,
}

// In PlanFeatureMap.FeatureKeyMap:
["telemedicine"] = PlanFeature.Telemedicine,
```

**Step 3 — Gate UI (`_ClinicLayout.cshtml`):**
```html
@if (_user?.HasFeature(PlanFeature.Telemedicine) == true)
{
    <a class="sidebar-item" href="#">
        <i class="bi bi-camera-video"></i>
        <span>استشارة عن بعد</span>
    </a>
}
```
