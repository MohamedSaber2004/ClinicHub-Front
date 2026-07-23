# Permission-Based Subscription System

## Overview

Each subscription plan now has **granular permissions** stored in a `PlanPermissions` table. The old binary `[RequireSubscription]` (on/off) is replaced with `[RequirePlanPermission]` that checks if the clinic's active plan includes a specific permission.

## Plan Permission Matrix

| Permission | Basic | Standard | Premium |
|---|---|---|---|
| ManageAppointments | ✓ | ✓ | ✓ |
| PatientRecords | ✓ | ✓ | ✓ |
| BasicReports | ✓ | ✓ | ✓ |
| OnlineBooking | ✓ | ✓ | ✓ |
| ManageStaff | ✓ | ✓ | ✓ |
| ManageDoctors | ✓ | ✓ | ✓ |
| AdvancedReports | — | ✓ | ✓ |
| MarketingTools | — | — | ✓ |
| PrioritySupport | — | — | ✓ |

## New Domain Types

### `SubscriptionPermission` enum (`Domain/Enums/`)

```csharp
[Flags]
public enum SubscriptionPermission
{
    None = 0,
    ManageAppointments = 1,
    PatientRecords = 2,
    BasicReports = 4,
    AdvancedReports = 8,
    MarketingTools = 16,
    PrioritySupport = 32,
    ManageStaff = 64,
    ManageDoctors = 128,
    OnlineBooking = 256,
    All = ~0
}
```

### `PlanPermission` entity (`Domain/Entities/`)

```csharp
public class PlanPermission : BaseEntity<Guid>
{
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public SubscriptionPermission Permission { get; set; }
}
```

`Plan.Permissions` navigation was added to the existing `Plan` entity.

## Using the Action Filter

### Server-side: Guard controller endpoints

```csharp
// Class-level — all actions require this permission
[ApiVersion("1.0")]
[RoleAuthorize(nameof(UserType.ClinicOwner))]
[RequirePlanPermission(SubscriptionPermission.MarketingTools)]
public class AdvertisementsController : BaseApiController
{
}

// Action-level — specific endpoint requires this permission
[HttpGet]
[Route(ApiRoutes.ClinicManagement.Dashboard)]
[RoleAuthorize(nameof(UserType.ClinicOwner))]
[RequirePlanPermission(SubscriptionPermission.BasicReports)]
public async Task<IActionResult> Dashboard()
{
}
```

Multiple permissions can be stacked (all must pass):

```csharp
[RequirePlanPermission(SubscriptionPermission.AdvancedReports)]
[RequirePlanPermission(SubscriptionPermission.PrioritySupport)]
```

### Filter behavior

The filter loads the clinic's active subscription → loads the Plan → checks if the plan's `PlanPermissions` contains the required permission.

| Condition | Response |
|---|---|
| No clinic ID | 403 "Clinic not found" |
| No active subscription | 403 "Active subscription required" |
| Plan lacks permission | 403 "Your current plan does not include this feature" |
| Permission found | Pass — request continues |

## API: Subscription Response Includes Permissions

`GET /api/v1/subscriptions/my` now returns permissions:

```json
{
  "id": "guid",
  "planName": "Basic",
  "permissions": [
    "ManageAppointments",
    "PatientRecords",
    "BasicReports",
    "ManageStaff",
    "ManageDoctors",
    "OnlineBooking"
  ],
  "isActive": true,
  ...
}
```

`GET /api/v1/plans` also returns permissions per plan.

## Frontend Integration

### 1. Fetch and cache permissions on login

```js
async function fetchMyPermissions() {
  const token = localStorage.getItem('accessToken');
  const res = await fetch('/api/v1/subscriptions/my', {
    headers: { Authorization: `Bearer ${token}` }
  });
  const body = await res.json();
  const permissions = body.data?.permissions ?? [];
  localStorage.setItem('myPermissions', JSON.stringify(permissions));
  return permissions;
}
```

### 2. Check permissions in UI

```js
function hasPermission(name) {
  const perms = JSON.parse(localStorage.getItem('myPermissions') || '[]');
  return perms.includes(name);
}

// Usage
if (hasPermission('AdvancedReports')) {
  // Show advanced reports feature
} else {
  // Show locked state with upgrade prompt
}
```

### 3. Locked feature UX

```jsx
function FeatureCard({ name, requiredPermission, children }) {
  const hasAccess = hasPermission(requiredPermission);

  if (!hasAccess) {
    return (
      <div className="card card-locked" onClick={showUpgradeModal}>
        <span className="lock-icon">🔒</span>
        <p>{name}</p>
        <small>Upgrade to access</small>
      </div>
    );
  }

  return <div className="card">{children}</div>;
}
```

## Adding a New Permission

1. Add value to `SubscriptionPermission` enum (powers of 2)
2. Add `PlanPermission` row for each plan that should have it (via migration SQL or DataSeeder)
3. Apply `[RequirePlanPermission(NewPermission)]` to the relevant controller/action

## Relationship to `[RoleAuthorize]`

`[RequirePlanPermission]` is **orthogonal** to `[RoleAuthorize]`. They work together:

```csharp
// User must be ClinicOwner AND their plan must include ManageStaff
[RoleAuthorize(nameof(UserType.ClinicOwner))]
[RequirePlanPermission(SubscriptionPermission.ManageStaff)]
```

Both must pass for the request to proceed.
