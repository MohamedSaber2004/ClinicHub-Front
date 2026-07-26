# Staff Dashboard — API Integration Guide

Base URL: `/api/v{version}` (current: `v1`)

All responses are wrapped in `ApiResponse<T>`:

```json
{
  "success": true,
  "data": { ... },
  "message": "...",
  "errors": {}
}
```

All endpoints require `Authorization: Bearer {token}` header with a **Staff** user role.

---

## Root Cause Analysis — Status Not Showing / Deserialization Failures

The following 4 root causes were identified and fixed across backend and frontend:

| # | Root Cause | File (Backend Ref) | Fix Owner | Status |
|---|-----------|-------------------|-----------|--------|
| 1 | `PagginatedResult.Items` returned as a JSON object (`{...}`) instead of array `[...]` | `PagginatedResult.cs:10` | Backend + Frontend | ✅ Fixed |
| 2 | `CurrentClinicId` null → empty result with wrong pageSize | `CurrentUserService.cs:16` / Handler:25 | Backend | ✅ Fixed |
| 3 | Intermediary layer re-serialized response to PascalCase, breaking camelCase contract | `StaffAppointmentDto.cs:11-13` | Backend (fix intermediary) | ✅ Fixed |
| 4 | `AppointmentDate == DateTime.Today` misses time-drifted rows | `GetStaffAppointmentsQueryHandler.cs:39-40` | Backend | ✅ Fixed |

### Detail: How Each Fix Affects Frontend

**#1 — `items` as object** (Frontend fix in `StaffDashboardService.cs:162-175`):  
`GetAppointmentsAsync` now uses resilient JObject extraction. If `items` is an array → deserialize directly. If `items` is an object → extract via `.Properties()`. Pagination metadata (`pageNumber`, `pageSize`, `totalCount`) read with `dataToken[key] ?? dataToken[PascalKey]` fallback.

**#3 — PascalCase re-serialization** (Frontend fix across all `ReponseModels/*.cs`):  
All DTO properties now carry dual JSON attributes — `[JsonPropertyName("camelCase")]` for System.Text.Json AND `[JsonProperty("camelCase")]` for Newtonsoft.Json. This makes camelCase matching explicit regardless of the contract resolver.

**#1 + #3 combined effect** (before fix):  
Backend sent `{data: {items: {1: {...}, 2: {...}}}}` with object-formatted items + intermediary re-serialized to PascalCase. Frontend's fragile `ToObject<PagginatedResult<T>>()` + `CamelCasePropertyNamesContractResolver` → both failures cascaded → zero data loaded.

---

## ⚠️ Critical Requirements (Common Root Causes)

### 1. ALL field names must be **camelCase**

| ❌ Wrong (backend returns) | ✅ Correct (frontend expects) |
|---------------------------|------------------------------|
| `"Status": "pending"` | `"status": "pending"` |
| `"StatusLabel": "قيد الانتظار"` | `"statusLabel": "قيد الانتظار"` |
| `"Items": [...]` | `"items": [...]` |
| `"TotalPages": 3` | `"totalPages": 3` |
| `"Data": {...}` | `"data": {...}` |
| `"Success": true` | `"success": true` |

The C# service uses `CamelCasePropertyNamesContractResolver`. If the backend returns PascalCase (`Status`, `Items`, `Data`), **ALL fields will be null** — no data loads, no errors shown.

### 2. Paginated endpoints MUST return `items` as a JSON **array** (`[...]`), not an object (`{...}`)

If `items` is an object instead of an array, the error below appears. See [Common Errors](#common-errors--diagnosis) for the exact message.

### 3. Every item MUST include `statusLabel` and `statusClass` for status badges to render

If the backend returns only `status` (e.g., `"status": "pending"`) without `statusLabel` and `statusClass`, the status column will render as an **empty badge**. The frontend has a fallback mapping (see [Status Mapping Table](#appointments-list)), but the backend providing `statusLabel` + `statusClass` is preferred.

---

## Common Errors & Diagnosis

### Error 1: `Cannot deserialize object into type 'List<StaffQueueItemDto>' at Path 'data.items'`

**Full message:**
```
Cannot deserialize the current JSON object into type 'List<StaffQueueItemDto>'
because the type requires a JSON array to deserialize correctly.
Path 'data.items', line 1, position 44.
```

**Root cause**: The `GET /staff/queue` endpoint returns `data` with a nested `items` field, but the `items` value is a JSON **object** (`{}`) instead of a JSON **array** (`[]`).

**Check your endpoint response at** `GET /api/v1/staff/queue?pageNumber=1&pageSize=10`:

```json
{
  "success": true,
  "data": {
    "items": {        ← ❌ MUST be an array [...], not object {...}
      "1": {...},
      "2": {...}
    }
  }
}
```

**Fix**: Change `items` to a JSON array:

```json
{
  "success": true,
  "data": {
    "items": [        ← ✅ Correct: JSON array
      { "queueNumber": 1, ... },
      { "queueNumber": 2, ... }
    ]
  }
}
```

If you intentionally want the queue to support pagination, include `totalPages`, `totalCount`, `pageNumber`, `pageSize` at the same level as `items` inside `data`.

---

### Error 2: `Error converting value {null} to type 'System.Boolean' at Path 'data'`

**Root cause**: The endpoint returns `"data": null` while the HTTP status is `200 OK`. The frontend service receives a 200 status but can't deserialize `null` into the expected `data` type.

**Check your endpoint response**: It probably looks like:

```json
{
  "success": false,
  "data": null,
  "message": "Clinic Id is required"
}
```

**Fix either**:
- **(Preferred)** Return proper error HTTP status (`400 Bad Request` or `401 Unauthorized`) instead of `200 OK`
- OR ensure the endpoint is called with a valid clinic context (JWT token containing clinic ID)

---

### Error 3: Status column shows empty / no badge text

**Root cause**: The endpoint returns items without `statusLabel` and `statusClass` fields, or returns status as an **integer** (e.g., `"status": 1`) instead of a string.

**Check your items**: Each item should include ALL three fields:

```json
{
  "status": "pending",        ← must be string, not integer
  "statusLabel": "قيد الانتظار",  ← Arabic display text
  "statusClass": "badge-warning"  ← CSS class for coloring
}
```

**Valid status values and their frontend labels:**

| `status` string | `statusLabel` | `statusClass` |
|----------------|---------------|---------------|
| `"pending"` | قيد الانتظار | `badge-warning` |
| `"confirmed"` | مؤكد | `badge-success` |
| `"cancelled"` | ملغي | `badge-danger` |
| `"completed"` | منتهي | `badge-info` |
| `"in-progress"` | قيد الكشف | `badge-primary` |
| `"waiting"` | في الانتظار | `badge-warning` |
| `"registered"` | تم التسجيل | `badge-info` |
| `"accepted"` | مقبول | `badge-success` |
| `"rejected"` | مرفوض | `badge-danger` |

**The frontend has a fallback mapper** that converts integer status (0–7) or missing labels automatically. But providing proper strings avoids reliance on fallback logic.

---

### Data Flow Summary

```
Browser (Razor View)
    ↓  GET /Staff/Appointments?pageNumber=1&pageSize=10
StaffController.Appointments()
    ↓  calls service
StaffDashboardService.GetAppointmentsAsync(...)
    ↓  HTTP GET
Doctory Backend API → /api/v1/staff/appointments?pageNumber=1&pageSize=10
    ↓  Response (must be camelCase, success/data wrapper)
ASP.NET deserializes → returns PagginatedResult<StaffAppointmentDto>
    ↓  stored in ViewBag
Razor View renders table + _Pagination.cshtml
```

---

## Paginated Endpoints — Common Contract

Two endpoints return paginated data using query parameters. The response shape is identical:

### Request Query Parameters

| Param | Type | Default | Max | Description |
|-------|------|---------|-----|-------------|
| `pageNumber` | int | `1` | — | Page index (1-based) |
| `pageSize` | int | `10` (Appts) / `10` (Queue) | `100` | Items per page |

### Response Shape

```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3,
    "totalCount": 22,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

### Field Reference

| Field | Type | Notes |
|-------|------|-------|
| `items` | array | Page of items |
| `pageNumber` | int | Current page (1-based) |
| `pageSize` | int | Items per page |
| `totalPages` | int | Total pages available |
| `totalCount` | int | Total items across all pages |
| `hasPreviousPage` | bool | `false` when on page 1 |
| `hasNextPage` | bool | `false` when on last page |

### Frontend Rendering Rules

- `totalPages <= 0` or `totalCount === 0` → hide pagination entirely, show "no data" state
- `totalPages === 1` → show static page indicator "1" (not clickable)
- `totalPages > 1` → show clickable page links (`« 1 2 3 … »`)
- `hasPreviousPage` → enable/disable "Previous" button
- `hasNextPage` → enable/disable "Next" button

---

## 1. Dashboard Stats

**`GET /api/v1/staff/dashboard/stats`** *(no pagination)*

### Response `200 OK`

```json
{
  "success": true,
  "data": {
    "totalAppointments": 24,
    "checkedIn": 18,
    "waiting": 6,
    "completed": 8
  }
}
```

| Field | Meaning |
|-------|---------|
| `totalAppointments` | All appointments today |
| `checkedIn` | Confirmed / Accepted (checked-in) |
| `waiting` | Pending / Reserved (not yet checked) |
| `completed` | Completed |

---

## 2. Appointments List — **Paginated**

**`GET /api/v1/staff/appointments?pageNumber=1&pageSize=10&status=&date=&patientName=`**

### Query Parameters

| Param | Type | Required | Notes |
|-------|------|----------|-------|
| `pageNumber` | int | No | Default `1` |
| `pageSize` | int | No | Default `10`, max `100` |
| `status` | int | No | `0=Pending, 1=Confirmed, 2=Cancelled, 3=Completed, 4=Reserved, 5=NoShow, 6=Accepted, 7=Rejected` |
| `date` | string `yyyy-MM-dd` | No | Defaults to today |
| `patientName` | string | No | Partial match (patient name or booker name) |

### Response `200 OK`

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "patient": {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "name": "محمد عمر",
          "initial": "م"
        },
        "doctor": {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "name": "د. سارة أحمد",
          "specialty": "أمراض القلب"
        },
        "specialty": "أمراض القلب",
        "date": "2026-07-26",
        "time": "09:00",
        "status": "pending",
        "statusLabel": "قيد الانتظار",
        "statusClass": "badge-warning",
        "phone": "0501112222"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3,
    "totalCount": 22,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

### Status → Frontend Mapping

| `status` value | `statusLabel` | `statusClass` | Action Available |
|---|---|---|---|
| `"pending"` | قيد الانتظار | `badge-warning` | Approve / Reject |
| `"confirmed"` | مؤكد | `badge-success` | Check-in |
| `"cancelled"` | ملغي | `badge-danger` | — (read-only) |
| `"completed"` | منتهي | `badge-info` | — (read-only) |

### Filtering by Status

The `status` filter accepts an **integer** matching the `AppointmentStatus` enum:

```
Pending = 0, Confirmed = 1, Cancelled = 2, Completed = 3,
Reserved = 4, NoShow = 5, Accepted = 6, Rejected = 7
```

Example: `?status=0` returns only pending appointments.

---

## 3. Approve Appointment

**`PUT /api/v1/staff/appointments/{id}/approve`** *(no pagination)*

### Path Parameter

| Param | Type | Description |
|-------|------|-------------|
| `id` | GUID | Appointment ID |

### Response `200 OK`

```json
{
  "success": true,
  "data": true,
  "message": "Appointments.Accepted"
}
```

### Errors

- `400` — Clinic context missing, not authorized, or appointment not in a respondable state
- `404` — Appointment not found

---

## 4. Reject Appointment

**`PUT /api/v1/staff/appointments/{id}/reject`** *(no pagination)*

### Path Parameter

| Param | Type | Description |
|-------|------|-------------|
| `id` | GUID | Appointment ID |

### Request Body

```json
{
  "reason": "الموعد غير مناسب"
}
```

| Field | Type | Required |
|-------|------|----------|
| `reason` | string | No (max 500 chars) |

### Response `200 OK`

```json
{
  "success": true,
  "data": true,
  "message": "Appointments.Rejected"
}
```

---

## 5. Check-In Patient

**`PUT /api/v1/staff/appointments/{id}/check-in`** *(no pagination)*

Transitions the appointment from `Accepted`/`Reserved` → `Confirmed`.

### Response `200 OK`

```json
{
  "success": true,
  "data": true,
  "message": "Appointments.CheckedIn"
}
```

---

## 6. Complete Appointment

**`PUT /api/v1/staff/appointments/{id}/complete`** *(no pagination)*

Transitions the appointment from `Accepted`/`Confirmed` → `Completed`.

### Response `200 OK`

```json
{
  "success": true,
  "data": true,
  "message": "Appointments.Completed"
}
```

---

## 7. Queue — **Paginated**

**`GET /api/v1/staff/queue?pageNumber=1&pageSize=10`**

### Query Parameters

| Param | Type | Required | Notes |
|-------|------|----------|-------|
| `pageNumber` | int | No | Default `1` |
| `pageSize` | int | No | Default `10`, max `100` |

### Response `200 OK`

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "queueNumber": 1,
        "patient": {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "name": "محمد عمر",
          "initial": "م"
        },
        "doctor": {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "name": "د. سارة أحمد",
          "specialty": "أمراض القلب"
        },
        "time": "09:00",
        "status": "in-progress",
        "statusLabel": "قيد الكشف",
        "statusClass": "badge-primary"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 2,
    "totalCount": 12,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

**Note:** `queueNumber` is sequential across the **entire** queue (not per page).  
Example: Page 2 will have `queueNumber: 11, 12, 13, …`

### Queue Status Mapping

| `status` | `statusLabel` | `statusClass` | Action |
|---|---|---|---|
| `"registered"` | تم التسجيل | `badge-info` | Check-in |
| `"waiting"` | في الانتظار | `badge-warning` | Check-in |
| `"in-progress"` | قيد الكشف | `badge-primary` | Complete |
| `"completed"` | مكتمل | `badge-success` | — (read-only) |

---

## 8. Register Walk-In Patient

**`POST /api/v1/staff/patients/register`** *(no pagination)*

### Request Body

```json
{
  "fullName": "أحمد محمد",
  "phoneNumber": "0501234567",
  "email": null,
  "age": 30,
  "gender": 1,
  "doctorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clinicId": "00000000-0000-0000-0000-000000000000",
  "appointmentDate": "2026-07-26T00:00:00",
  "startTime": "09:00",
  "endTime": "09:30",
  "appointmentType": 0,
  "complaint": "ألم في الأسنان",
  "chronicDiseases": null
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `fullName` | string | ✅ | |
| `phoneNumber` | string | ✅ | |
| `email` | string | ❌ | |
| `age` | int | ❌ | |
| `gender` | int | ❌ | `1=Male, 2=Female` |
| `doctorId` | GUID | ✅ | |
| `clinicId` | GUID | ✅ | Send `00000000-0000-0000-0000-000000000000` to auto-use staff clinic |
| `appointmentDate` | ISO date `yyyy-MM-dd` | ✅ | |
| `startTime` | string `HH:mm` | ✅ | |
| `endTime` | string `HH:mm` | ✅ | |
| `appointmentType` | int | ✅ | `0=Examination, 1=FollowUp` |
| `complaint` | string | ✅ | |
| `chronicDiseases` | string | ❌ | |

### Response `201 Created`

```json
{
  "success": true,
  "data": {
    "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "queueNumber": 7,
    "message": "تم تسجيل المريض بنجاح"
  }
}
```

---

## 9. Doctor List (Dropdown)

**`GET /api/v1/staff/doctors`** *(no pagination)*

### Response `200 OK`

```json
{
  "success": true,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "د. سارة أحمد",
      "specialty": "أمراض القلب"
    },
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "د. عمار السيد",
      "specialty": "جلدية"
    }
  ]
}
```

Returns all doctors in the staff user's clinic.

---

## 10. Doctor Schedule

**`GET /api/v1/staff/doctors/{doctorId}/schedule?date=2026-07-26`** *(no pagination)*

### Path & Query Parameters

| Param | Type | Required | Notes |
|-------|------|----------|-------|
| `doctorId` | GUID | ✅ (path) | |
| `date` | string `yyyy-MM-dd` | ❌ (query) | Defaults to today |

### Response `200 OK`

```json
{
  "success": true,
  "data": {
    "doctor": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "د. سارة أحمد",
      "specialty": "أمراض القلب"
    },
    "date": "2026-07-26",
    "appointments": [
      {
        "patient": {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "name": "محمد عمر",
          "initial": "م"
        },
        "time": "09:00",
        "statusLabel": "مؤكد",
        "statusClass": "badge-success"
      }
    ]
  }
}
```

---

## Error Response Shape

All errors return `success: false`:

```json
{
  "success": false,
  "message": "Appointment not found",
  "errors": {
    "AppointmentId": ["Appointment not found"]
  },
  "statusCode": 404
}
```

### Common HTTP Status Codes

| Code | Meaning |
|------|---------|
| `200` | Success |
| `201` | Created (walk-in registration) |
| `400` | Bad request — validation error or business rule violation. Check `errors` for field-level details |
| `401` | Unauthorized — missing or invalid token |
| `403` | Forbidden — wrong user role or missing plan permission |
| `404` | Resource not found |

---

## Quick Reference — All Endpoints

| # | Method | Route | Paginated | Query Params |
|---|--------|-------|-----------|-------------|
| 1 | `GET` | `/staff/dashboard/stats` | ❌ | — |
| 2 | `GET` | `/staff/appointments` | ✅ | `pageNumber`, `pageSize`, `status`, `date`, `patientName` |
| 3 | `PUT` | `/staff/appointments/{id}/approve` | ❌ | — |
| 4 | `PUT` | `/staff/appointments/{id}/reject` | ❌ | — (body: `reason`) |
| 5 | `PUT` | `/staff/appointments/{id}/check-in` | ❌ | — |
| 6 | `PUT` | `/staff/appointments/{id}/complete` | ❌ | — |
| 7 | `GET` | `/staff/queue` | ✅ | `pageNumber`, `pageSize` |
| 8 | `POST` | `/staff/patients/register` | ❌ | — (body) |
| 9 | `GET` | `/staff/doctors` | ❌ | — |
| 10 | `GET` | `/staff/doctors/{doctorId}/schedule` | ❌ | `date` |
