# Frontend Integration Guide — Subscription/Permission System

## Overview

The frontend enforces **plan-based feature access** by reading each plan's `features` JSON array (from the plans API) and mapping each feature string to a `PlanFeature` flag. Sidebar items and views check `CurrentUserContext.HasFeature(PlanFeature.X)` before rendering.

---

## 1. API Endpoints

### `GET /api/v1/plans` (public, active plans)

Returns **2 plans** (Basic & Advanced):

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
    "features": "[\"appointments\",\"patient_records\",\"basic_reports\",\"staff_management\",\"doctor_management\",\"online_booking\"]",
    "isActive": true,
    "sortOrder": 1
  },
  {
    "id": "guid",
    "name": "Advanced",
    "nameAr": "متقدمة",
    "priceMonthly": 1500,
    "priceYearly": 15000,
    "maxDoctors": null,
    "maxStaff": null,
    "features": "[\"appointments\",\"patient_records\",\"basic_reports\",\"advanced_reports\",\"marketing_tools\",\"priority_support\",\"staff_management\",\"doctor_management\",\"online_booking\"]",
    "isActive": true,
    "sortOrder": 2
  }
]
```

**Rules:**
- `features` is a **JSON string** — the frontend must `JSON.parse()` it to get the array
- `maxDoctors` / `maxStaff` = `null` means **unlimited**
- `nameAr` is used for Arabic UI display

---

### `GET /api/v1/subscriptions/my` (auth required, ClinicOwner)

```json
{
  "id": "guid",
  "clinicId": "guid",
  "clinicName": "عيادة السلام الطبي",
  "planId": "guid",
  "planName": "متقدمة",
  "period": 0,
  "startDate": "2026-07-23T00:00:00",
  "endDate": "2027-07-23T00:00:00",
  "status": 0,
  "amount": 15000,
  "paidAt": "2026-07-23T00:00:00",
  "isActive": true,
  "permissions": ["ManageAppointments", "PatientRecords", ...]
}
```

The frontend uses `planId` to look up the plan details from `/api/v1/plans` and reads `features` + `maxDoctors` + `maxStaff` from it.

**Note about `permissions`:** This is a `List<string>` of `SubscriptionPermission` enum names from the plan's `PlanPermissions` table. It's provided as a convenience but the frontend should primarily use the `features` string from the plan for feature resolution.

---

### `GET /api/v1/auth/profile` (auth required)

Currently returns user info **without** plan data. The frontend should:
1. Call `/api/v1/subscriptions/my` to get the current subscription
2. Use `planId` to find the matching plan from the cached plans list
3. Resolve features from that plan

---

## 2. Feature Key → PlanFeature Mapping

Maintain this **hard-coded dictionary** on the frontend (e.g. `Roles.cs:PlanFeatureMap`):

| API Feature Key          | PlanFeature Flag           |
|--------------------------|----------------------------|
| `appointments`           | `ManageAppointments`       |
| `patient_records`        | `ManagePatientRecords`     |
| `basic_reports`          | `BasicReports`             |
| `advanced_reports`       | `AdvancedReports`          |
| `marketing_tools`        | `MarketingTools`           |
| `priority_support`       | `PrioritySupport`          |
| `online_booking`         | `OnlineBooking`            |
| `staff_management`       | `ManageStaff`              |
| `doctor_management`      | `ManageDoctors`            |

**→ `sms_notifications` should be ignored.** It is not a plan feature on the backend; the frontend should not map it or display it anywhere.

**If you add a new feature key on the backend, you MUST add a corresponding entry in the frontend's `PlanFeatureMap`.**

---

## 3. Feature Matrix (What Each Plan Has)

| Feature                | feature key            | Basic | Advanced |
|------------------------|------------------------|-------|----------|
| إدارة المواعيد         | `appointments`         | ✓     | ✓        |
| السجلات الطبية         | `patient_records`      | ✓     | ✓        |
| تقارير أساسية          | `basic_reports`        | ✓     | ✓        |
| الحجز والدفع أونلاين   | `online_booking`       | ✓     | ✓        |
| إدارة الموظفين         | `staff_management`     | ✓     | ✓        |
| إدارة الأطباء          | `doctor_management`    | ✓     | ✓        |
| تقارير متقدمة          | `advanced_reports`     | —     | ✓        |
| أدوات تسويقية          | `marketing_tools`      | —     | ✓        |
| دعم ذو أولوية          | `priority_support`     | —     | ✓        |

**Not present in any plan (ignore on frontend):** `sms_notifications`

---

## 4. Frontend Initialization Flow

On page load (e.g. `ClinicController.OnActionExecuting`):

1. **Fetch subscription** → `GET /api/v1/subscriptions/my`
2. **Fetch plans** → `GET /api/v1/plans` (cache this)
3. **Match plan** → Find plan where `plan.id === subscription.planId`
4. **Parse features** → `JSON.parse(plan.features)` → `string[]`
5. **Map features** → Use the `PlanFeatureMap` dictionary to convert strings → `PlanFeature` flags
6. **Store in context** → Set `CurrentUserContext.PlanFeatures`, `CurrentUserContext.MaxDoctors`, `CurrentUserContext.MaxStaff`, `CurrentUserContext.PlanName`

---

## 5. UI Gating

### Sidebar (`_ClinicLayout.cshtml`)
Each sidebar item checks `_user.HasFeature(PlanFeature.X)` before rendering:

```razor
@if (_user?.HasFeature(PlanFeature.MarketingTools) == true)
{
    <li><a href="/Clinic/Marketing"><i class="bi bi-megaphone"></i> أدوات تسويقية</a></li>
}
@if (_user?.HasFeature(PlanFeature.AdvancedReports) == true)
{
    <li><a href="/Clinic/Reports"><i class="bi bi-file-bar-graph"></i> تقارير متقدمة</a></li>
}
```

### Dashboard (`Clinic/Index.cshtml`)
Show a plan-info bar indicating current plan name, doctor/staff limits, and enabled features.

### Limits Display
- **Staff page:** Show a notice when `MaxStaff` has a numeric value (e.g., "You can add up to 5 staff members")
- **Doctors page:** Show a notice when `MaxDoctors` has a numeric value
- When value is `null`, show nothing (unlimited)

---

## 6. Important Notes

- `sms_notifications` is **not a plan feature** — remove any UI or mapping related to it
- The backend enforces permissions via `[RequirePlanPermission]` filter on controllers — returns `403` if the plan doesn't include the feature
- Plans are cached on the frontend after initial fetch; re-fetch on user action or periodic refresh
- Future plans may add more features — the frontend `PlanFeatureMap` must be updated in sync with the backend