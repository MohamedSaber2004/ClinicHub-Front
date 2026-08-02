# Doctor Dashboard — Frontend Integration Guide

> **Purpose:** Explains how every Doctor Dashboard frontend page integrates with the backend API endpoints implemented in `DoctorDashboardController`. This complements [DOCTOR-DASHBOARD-ENDPOINTS-README.md](./DOCTOR-DASHBOARD-ENDPOINTS-README.md).

---

## Table of Contents

1. [Global Conventions](#1-global-conventions)
2. [Authentication & Cookies](#2-authentication--cookies)
3. [Page 1 — Overview (`/Doctor`)](#3-page-1--overview-doctor)
4. [Page 2 — My Appointments (`/Doctor/Appointments`)](#4-page-2--my-appointments-doctoroappointments)
5. [Page 3 — My Patients (`/Doctor/Patients`)](#5-page-3--my-patients-doctorpatients)
6. [Page 4 — Availability (`/Doctor/Availability`)](#6-page-4--availability-doctoravailability)
7. [Response Shape Reference](#7-response-shape-reference)
8. [Status Code Mapping](#8-status-code-mapping)
9. [Error Handling Pattern](#9-error-handling-pattern)

---

## 1. Global Conventions

| Property | Value |
|----------|-------|
| **Base URL** | `{origin}/api/v1` |
| **Auth** | HttpOnly cookie `AccessToken` sent automatically by the browser. For JavaScript `fetch`, always pass `credentials: 'include'`. |
| **Doctor identity** | **Never** send a `doctorId` parameter — the backend resolves it from the JWT in the cookie. |
| **Content-Type** | `application/json` for all POST/PUT bodies. |
| **Accept-Language** | `ar` (default) or `en` — controls the language of user-facing error messages. |
| **Response envelope** | All responses are wrapped in `ApiResponse<T>` — see §7. |

### JavaScript fetch template

```js
async function apiFetch(url, options = {}) {
    const res = await fetch(url, {
        ...options,
        credentials: 'include',                 // send HttpOnly cookie
        headers: {
            'Content-Type': 'application/json',
            'Accept-Language': 'ar',
            ...(options.headers ?? {})
        }
    });
    if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw new Error(err?.errors?.[0] ?? `HTTP ${res.status}`);
    }
    return res.json();   // { isSuccess, data, errors }
}
```

---

## 2. Authentication & Cookies

The backend sets `AccessToken` as a secure, HttpOnly cookie on successful login (`POST /auth/login-web`). The frontend does **not** need to attach any `Authorization` header — the browser sends the cookie automatically on every same-origin request.

> **CSRF Note:** Since the cookie is HttpOnly and all state-mutating endpoints require the cookie to be present, an additional CSRF token is not currently required. If the deployment moves cross-origin, revisit this.

---

## 3. Page 1 — Overview (`/Doctor`)

**View file:** `Views/Doctor/Index.cshtml`

### 3.1 KPI Stat Cards

**`GET /api/v1/doctors/dashboard/stats`**

Called on page load (no query params needed).

```js
const { data } = await apiFetch('/api/v1/doctors/dashboard/stats');

// Map to UI stat cards:
// ┌─────────────────────────────────────────────────────────────┐
// │ Card 1 → data.todayAppointmentsCount   "مواعيد اليوم"      │
// │ Card 2 → data.totalPatientsCount       "إجمالي المرضى"     │
// │ Card 3 → data.pendingAppointmentsCount "في الانتظار"       │
// │ Card 4 → data.completedAppointmentsCount "مكتملة"          │
// └─────────────────────────────────────────────────────────────┘
```

**Response `data` fields used by the Overview page:**

| Field | Type | UI Usage |
|-------|------|----------|
| `todayAppointmentsCount` | `int` | "مواعيد اليوم" card |
| `totalPatientsCount` | `int` | "إجمالي المرضى" card |
| `pendingAppointmentsCount` | `int` | "في الانتظار" card |
| `completedAppointmentsCount` | `int` | "مكتملة" (all-time) card |
| `nextAppointment` | `AppointmentDto \| null` | "الموعد القادم" widget |

> Extended fields (`acceptedAppointments`, `cancelledAppointments`, `totalPatientsThisWeek`) are also available for more detailed analytics widgets.

### 3.2 Recent Appointments Table

**`GET /api/v1/doctors/dashboard/recent-appointments?limit=5`**

Called on page load to populate the top-5 overview table.

```js
const { data } = await apiFetch('/api/v1/doctors/dashboard/recent-appointments?limit=5');
// data → DoctorAppointmentDto[]   (see §7)
renderRecentTable(data);
```

**Query params:**

| Param | Default | Notes |
|-------|---------|-------|
| `limit` | `5` | Max 50. Increase if the page shows more rows. |

---

## 4. Page 2 — My Appointments (`/Doctor/Appointments`)

**View file:** `Views/Doctor/Appointments.cshtml`

### 4.1 Load Paginated Appointments

**`GET /api/v1/doctors/appointments`**

```js
const params = new URLSearchParams({
    pageNumber: currentPage,
    pageSize: 10,
    // optional filters:
    status: selectedStatus ?? '',      // 0|1|2|3|4|5|6|7  (see §8)
    startDate: dateFrom ?? '',         // 'YYYY-MM-DD'
    endDate: dateTo ?? '',
    patientName: searchQuery ?? ''
});
const { data } = await apiFetch(`/api/v1/doctors/appointments?${params}`);
```

**Response `data`** → `PagginatedResult<DoctorAppointmentDto>`

```json
{
  "items": [ ... ],
  "totalCount": 42,
  "totalPages": 5,
  "pageNumber": 1,
  "pageSize": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### 4.2 Accept / Reject / Complete (Unified)

**`PUT /api/v1/doctors/appointments/{id}/status`**

This single endpoint handles all three status transitions. Send the appropriate integer status code:

```js
// Accept
await apiFetch(`/api/v1/doctors/appointments/${appointmentId}/status`, {
    method: 'PUT',
    body: JSON.stringify({ status: 1, notes: null })
});

// Reject with reason
await apiFetch(`/api/v1/doctors/appointments/${appointmentId}/status`, {
    method: 'PUT',
    body: JSON.stringify({ status: 2, notes: 'لا تتوفر أوقات مناسبة' })
});

// Complete
await apiFetch(`/api/v1/doctors/appointments/${appointmentId}/status`, {
    method: 'PUT',
    body: JSON.stringify({ status: 3, notes: null })
});
```

**Request body:**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `status` | `int` | ✅ | `1`=Accept, `2`=Reject/Cancel, `3`=Complete |
| `notes` | `string?` | ❌ | Sent to patient on rejection as cancellation reason |

> **Legacy endpoints** (`/accept`, `/reject`, `/complete`) remain available for backward compatibility but the unified `/status` endpoint is preferred for new frontend code.

### 4.3 Status Transition Rules (client-side guard)

| From status | Can Accept (→1) | Can Reject (→2) | Can Complete (→3) |
|-------------|:-:|:-:|:-:|
| Pending (0) | ✅ | ✅ | ❌ |
| Confirmed (1) | ✅ | ✅ | ✅ |
| Accepted (6) | ❌ | ❌ | ✅ |
| Any other | ❌ | ❌ | ❌ |

---

## 5. Page 3 — My Patients (`/Doctor/Patients`)

**View file:** `Views/Doctor/Patients.cshtml`

### 5.1 Load Paginated Patients

**`GET /api/v1/doctors/patients`**

Only patients who have at least one **completed** appointment with the doctor are returned.

```js
const params = new URLSearchParams({
    pageNumber: currentPage,
    pageSize: 10,
    search: searchQuery ?? ''    // searches name or phone
});
const { data } = await apiFetch(`/api/v1/doctors/patients?${params}`);
// data → PagginatedResult<DoctorPatientDto>
```

**`DoctorPatientDto` fields:**

| Field | Type | UI Usage |
|-------|------|----------|
| `userId` | `Guid` | Pass to history endpoint |
| `fullName` | `string` | Patient name column |
| `phoneNumber` | `string?` | Contact column |
| `age` | `int?` | Age column |
| `gender` | `int?` | `0`=Male, `1`=Female |
| `totalVisits` | `int` | Visits badge |
| `lastVisitDate` | `DateTime` | Last visit column |

### 5.2 Visit History Sub-Page

**View file:** `Views/Doctor/PatientHistory.cshtml`

**`GET /api/v1/doctors/patients/{patientId}/history`**

```js
// patientId comes from the selected row's `userId`
const params = new URLSearchParams({ pageNumber: 1, pageSize: 10 });
const { data } = await apiFetch(`/api/v1/doctors/patients/${patientId}/history?${params}`);
// data → PagginatedResult<PatientHistoryDto>
```

**`PatientHistoryDto` fields:**

| Field | Type | Notes |
|-------|------|-------|
| `appointmentId` | `Guid` | Row identifier |
| `appointmentDate` | `string` | `YYYY-MM-DD` |
| `startTime` / `endTime` | `string` | `HH:mm` |
| `appointmentType` | `int` | `0`=InClinic, `1`=Home |
| `status` | `int` | See §8 for status codes |
| `complaint` | `string` | Chief complaint |
| `chronicDiseases` | `string?` | Chronic conditions |
| `cancellationReason` | `string?` | Present only when cancelled |

---

## 6. Page 4 — Availability (`/Doctor/Availability`)

**View file:** `Views/Doctor/Availability.cshtml`

This page is **already fully integrated** (see [`README.md`](./README.md) §1). Summary:

| Action | Endpoint | Method |
|--------|----------|--------|
| Load weekly schedule | `/api/v1/doctors/availability` | `GET` |
| Replace whole week | `/api/v1/doctors/availability/week` | `PUT` |
| Add single slot | `/api/v1/doctors/availability` | `POST` |
| Update single slot | `/api/v1/doctors/availability/{id}` | `PUT` |
| Delete single slot | `/api/v1/doctors/availability/{id}` | `DELETE` |

> ⚠️ `id` must be a valid GUID belonging to the current doctor. Default/seeded rows (empty-GUID ids) must be stripped before saving.

---

## 7. Response Shape Reference

All endpoints wrap their payload in the same envelope:

```json
{
  "isSuccess": true,
  "data": { ... },
  "errors": []
}
```

On failure (`isSuccess: false`):

```json
{
  "isSuccess": false,
  "data": null,
  "errors": ["الرسالة بالعربية"]
}
```

> Always read `errors[0]` to display user-facing feedback.

### `DoctorAppointmentDto` (full fields)

```ts
interface DoctorAppointmentDto {
    id: string;                 // GUID
    bookedByUserId: string;
    bookedByUserName: string | null;
    bookedByUserPhone: string | null;
    appointmentDate: string;    // "YYYY-MM-DD"
    startTime: string;          // "HH:mm"
    endTime: string;            // "HH:mm"
    appointmentType: number;    // 0=InClinic, 1=Home
    status: number;             // see §8
    patientFullName: string;
    patientPhoneNumber: string;
    patientAge: number;
    patientGender: number;      // 0=Male, 1=Female
    complaint: string;
    chronicDiseases: string | null;
    cancellationReason: string | null;
    createdAt: string;          // ISO 8601
    clinicName: string | null;
}
```

---

## 8. Status Code Mapping

### `AppointmentStatus` enum

| Code | Name | Arabic UI Label | Notes |
|------|------|-----------------|-------|
| `0` | Pending | قيد الانتظار | Awaiting doctor action |
| `1` | Confirmed | مؤكد | Confirmed by system (e.g., paid) |
| `2` | Cancelled | ملغى | Patient cancelled |
| `3` | Completed | مكتمل | Visit concluded |
| `4` | Reserved | محجوز مؤقتاً | Payment hold in progress |
| `5` | NoShow | لم يحضر | Patient did not attend |
| `6` | Accepted | مقبول | Doctor accepted |
| `7` | Rejected | مرفوض | Doctor rejected |

### `UpdateAppointmentStatus` request codes

| Request `status` | Domain action | Allowed from statuses |
|-----------------|---------------|-----------------------|
| `1` | Accept | Pending, Reserved, Confirmed |
| `2` | Reject / Cancel | Pending, Reserved, Confirmed |
| `3` | Complete | Accepted, Confirmed |

---

## 9. Error Handling Pattern

```js
async function handleAction(appointmentId, statusCode, notes = null) {
    try {
        const { data } = await apiFetch(
            `/api/v1/doctors/appointments/${appointmentId}/status`,
            { method: 'PUT', body: JSON.stringify({ status: statusCode, notes }) }
        );
        if (data === true) {
            showToast('success', 'تم تحديث حالة الموعد');
            refreshAppointmentsList();
        }
    } catch (err) {
        showToast('error', err.message);
    }
}
```

### HTTP Status → UI Behaviour

| HTTP | Meaning | Frontend Action |
|------|---------|-----------------|
| `200` | Success | Refresh table / show success toast |
| `400` | Validation / business rule error | Show `errors[0]` to user |
| `401` | Session expired | Redirect to `/auth/login-web` |
| `403` | Forbidden (wrong role / no subscription) | Show "غير مصرح" message |
| `404` | Appointment not found | Show "الموعد غير موجود" message |
| `500` | Server error | Show generic "حدث خطأ، حاول مرة أخرى" |
