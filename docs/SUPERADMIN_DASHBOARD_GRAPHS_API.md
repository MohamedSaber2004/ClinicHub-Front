# SuperAdmin Dashboard — Graphs API Contract

Contract for the five dashboard graph endpoints consumed by `Views/Admin/Index.cshtml`
via `IAdminDashboardService`. All endpoints live under the SuperAdmin dashboard scope
(`[RoleAuthorize(SuperAdmin)]`), i.e. `api/v1/admin/dashboard/...`.

## Common query parameters

| Name          | Type     | Default | Notes                                              |
|---------------|----------|---------|----------------------------------------------------|
| `granularity` | string   | `day`   | `day` \| `week` \| `month`. Frontend currently sends `day`. |
| `fromDate`    | date     | today − N days (`yyyy-MM-dd`) |
| `toDate`      | date     | tomorrow (`yyyy-MM-dd`)       |

Every point's `period` string must be a short display label:
- day → `yyyy-MM-dd`
- week → `yyyy-MM-dd` (week start)
- month → `yyyy-MM`

## Endpoints

### 1. GET /api/v1/admin/dashboard/revenue-trend
Response `200`:
```json
{ "data": [ { "period": "2026-08-20", "revenue": 1500.00, "paymentsCount": 3 } ] }
```
Revenue = sum of successful payment amounts in the period (all payment types: subscriptions + ads orders + appointment fees).

### 2. GET /api/v1/admin/dashboard/clinics-growth
Response `200`:
```json
{ "data": [ { "period": "2026-08-20", "newClinics": 2, "totalClinics": 41 } ] }
```
New clinics by `CreatedAt`; running total of non-deleted clinics.

### 3. GET /api/v1/admin/dashboard/subscriptions-by-plan
Same `fromDate`/`toDate`, **no** granularity.
Response `200`:
```json
{
  "data": [
    { "planId": "…", "planName": "الأساسية", "subscriptionsCount": 12, "totalRevenue": 9600 }
  ]
}
```
Groups active subscriptions in range by plan.

### 4. GET /api/v1/admin/dashboard/users-growth
Response `200`:
```json
{ "data": [ { "period": "2026-08-20", "newUsers": 5, "totalUsers": 310 } ] }
```
New non-deleted users by `CreatedAt`; running total.

### 5. GET /api/v1/admin/dashboard/appointments-summary
Response `200`:
```json
{
  "data": [
    { "period": "2026-08-20", "completedCount": 8, "cancelledCount": 2, "pendingCount": 4 }
  ]
}
```
Counts grouped per period by appointment status:
- `completedCount` → `Completed`
- `cancelledCount` → `Cancelled`, `Rejected`, `NoShow`
- `pendingCount` → `Pending`, `Confirmed`, `Accepted`, `Reserved`

## Status: IMPLEMENTED (backend)
All five endpoints exist in `E:\ClinicHub`:
- Queries: `ClinicHub.Application/Features/Admin/Queries/{GetRevenueTrend,GetClinicsGrowth,GetSubscriptionsByPlan,GetUsersGrowth,GetAppointmentsSummary}/`
- Shared bucketing: `Queries/Common/GraphPeriodHelper.cs` (day/week/month, Cairo-local dates, gap-filled series, 400-day range cap)
- Routes: `ApiRoutes.AdminDashboard.*`; actions in `SuperAdminController`
- Subscriptions-by-plan counts currently **Active** subscriptions overlapping `[fromDate, toDate]`.

## Error handling
- Any non-success returns `{ "errors": ["رسالة عربية"], "statusCode": n }`.
- The frontend treats `404` on any graph endpoint as "not implemented yet" and renders an
  empty-state chart instead of an error — safe to ship endpoints incrementally.

## Suggested backend implementation notes
- Add route constants to `ApiRoutes.AdminDashboard` / `AdminDashboardExt`.
- One MediatR query + handler + validator per endpoint under
  `Features/Admin/Queries/<Name>/`, DTOs under `Features/Admin/DTOs/`,
  mapping profile entries in `AdminProfile.cs`.
- Keep handlers read-only; no new permissions required (SuperAdmin scope already enforced).
