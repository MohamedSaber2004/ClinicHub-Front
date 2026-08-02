# Superadmin Create Subscription — Backend API Contract (Required)

> **Audience:** Backend team (Doctory API).
> **Status:** ⚠️ **Required** — the superadmin dashboard's new **"إضافة اشتراك"** (Add Subscription)
> feature on the Subscription Management page (`/Admin/SubscriptionManagement`) is fully implemented
> in the frontend and calls the endpoint below. **It does not exist yet** — without it the feature
> returns an error until deployed.
> **Frontend implementation date:** 2026-08-02

---

## 1. The endpoint (one new route)

| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/api/v1/admin/dashboard/subscriptions` | Create an **active** subscription for a clinic (admin-initiated, no Paymob) |

- **Base URL:** `/api/v1`
- **Auth:** `Authorization: Bearer <token>` — requires the **SuperAdmin** role (`UserType.SuperAdmin`), otherwise `401`.
- **Language:** `Accept-Language: ar` (default) — messages localized (Arabic).
- **Response wrapper:** every response is `ApiResponse<T>`:

```json
{ "success": true, "data": { }, "message": "string", "errors": [], "statusCode": 201 }
```

---

## 2. Request body

```json
{
  "clinicId": "00000001-0000-0000-0000-000000000001",
  "planId": "b1f6c1a0-1111-1111-1111-111111111111",
  "period": 0,
  "startDate": "2026-08-02",
  "amount": 500.00
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `clinicId` | Guid | ✅ | The clinic that owns the subscription |
| `planId` | Guid | ✅ | The plan being granted |
| `period` | int | ✅ | `0` = monthly, `1` = yearly |
| `startDate` | date | ❌ | `YYYY-MM-DD`. Defaults to **today** when omitted |
| `amount` | decimal | ❌ | Overrides the plan price. Defaults to the plan's `PriceMonthly` / `PriceYearly` per `period` when omitted |

> The frontend sends `clinicId`, `planId`, `period`, `startDate` (amount is **not** sent — the
> backend should default to the plan price).

---

## 3. Success response — `201`

```json
{
  "success": true,
  "data": {
    "id": "a2c3d4e5-...-guid",
    "clinicId": "00000001-0000-0000-0000-000000000001",
    "clinicName": "مجمع عيادات السلام الطبي",
    "planId": "b1f6c1a0-...-guid",
    "planName": "Advanced",
    "period": 0,
    "startDate": "2026-08-02T00:00:00",
    "endDate": "2026-09-02T00:00:00",
    "status": 0,
    "amount": 500.00,
    "paidAt": "2026-08-02T10:00:00Z",
    "isActive": true
  },
  "message": "تم إنشاء الاشتراك بنجاح",
  "errors": [],
  "statusCode": 201
}
```

The `data` object must be a `SubscriptionDto` — the **same shape** returned by
`GET /api/v1/admin/dashboard/subscriptions` (the existing list endpoint), so the frontend table
can render it directly.

---

## 4. Validation rules (HTTP 400)

| Field | Rule |
|-------|------|
| `clinicId` | Required, must be an existing clinic → otherwise `404` |
| `planId` | Required, must be an existing **active** plan → otherwise `404` |
| `period` | Must be `0` or `1` |
| `startDate` | Optional; must not be a past date before **today** (or reject with a clear message) |
| `amount` | Optional; if provided must be `> 0` |

Error envelope (same as the rest of the API):

```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": { "ClinicId": ["Clinic id is required"] },
  "statusCode": 400
}
```

| Code | Meaning | Frontend action |
|------|---------|-----------------|
| `400` | Validation error | toast with `errors[0]` / `message` |
| `401` | Missing/invalid/expired admin token | redirect to login |
| `403` | Not SuperAdmin | toast with `message` |
| `404` | Clinic or plan not found | toast with `message` |

---

## 5. ⚠️ CRITICAL — sync with the clinic owner account (business requirement)

The whole point of this feature is that the superadmin grants a subscription **instead of** the
clinic owner paying online. The backend MUST therefore behave like a successful Paymob checkout:

1. **Create an ACTIVE subscription record** linked to the clinic (`ClinicId`), with
   `isActive: true` and `status: 0` (Active), `startDate` → `endDate` computed by `period`.
2. **Immediately reflect in the clinic owner dashboard** — `GET /api/v1/subscriptions/my`
   (consumed by the Clinic dashboard on **every** page load, `ClinicController.OnActionExecutionAsync`)
   must return this subscription, so `HasActivePlan = true` and all dashboard features unlock
   without any clinic-side action.
3. **Appear in the admin list** — `GET /api/v1/admin/dashboard/subscriptions` must return it
   (the frontend refreshes the table after creation).
4. **Optionally create a payment record** (type `Subscription`) for accounting consistency —
   recommended, so it shows in the Payments page with `إيرادات الاشتراكات`. The frontend does
   **not** call the manual-payment endpoint here; if a payment row is wanted, the backend should
   create it as part of this operation (or the superadmin records it separately via
   `POST /api/v1/admin/payments/manual`).

### Duplicate handling
- If the clinic already has an **active** subscription for the same plan, decide the behavior:
  - Recommended: **extend/refresh** the existing subscription (new `endDate` = `max(current endDate, today) + period`) OR
  - Reject with `400` and a localized message («العيادة لديها اشتراك نشط بالفعل»).
  Pick one and document it; the frontend shows whatever `message` the backend returns.

---

## 6. Relationship to existing endpoints (do not break)

| Endpoint | Status | Notes |
|----------|--------|-------|
| `GET /api/v1/admin/dashboard/subscriptions` | ✅ exists | List (status/plan/clinic filters + pagination) |
| `POST /api/v1/admin/dashboard/subscriptions` | ❌ **MISSING — this feature** | Must be added |
| `POST /api/v1/admin/dashboard/subscriptions/{id}/revoke` | ✅ exists | Revoke — unchanged |
| `POST /api/v1/admin/payments/manual` | ✅ exists | Manual cash payment — unchanged (separate from subscription creation) |
| `GET /api/v1/admin/dashboard/clinics` | ✅ exists | Clinics dropdown for the modal |
| `GET /api/v1/admin/plans` | ✅ exists | Plans dropdown for the modal |

---

## 7. How to verify after deployment

```bash
# Expect 401 when unauthenticated (route exists) — NOT 404
curl -s -o /dev/null -w "%{http_code}\n" \
  -X POST https://doctory-icare.runasp.net/api/v1/admin/dashboard/subscriptions

# With a SuperAdmin token — expect 201 + subscription data
curl -s -X POST \
  -H "Authorization: Bearer <SUPERADMIN_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{ "clinicId": "<CLINIC_ID>", "planId": "<PLAN_ID>", "period": 0 }' \
  https://doctory-icare.runasp.net/api/v1/admin/dashboard/subscriptions
```

Then log into the clinic owner dashboard — all pages must open (no redirect to the renewal page),
proving the subscription is active and synced.
