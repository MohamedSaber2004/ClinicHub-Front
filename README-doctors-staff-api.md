# Doctors & Staff Management — Frontend Integration

Both features are gated by plan permissions. The ClinicOwner must have an active subscription with the required feature.

| Feature | Permission | Plan Requirement |
|---------|-----------|-----------------|
| Doctors Management | `ManageDoctors` | Basic / Advanced |
| Staff Management | `ManageStaff` | Basic / Advanced |

---

## 1. Doctors Management

### 1.1 `GET /api/v1/admin/clinics/{clinicId}/doctors` — List doctors (paginated)

**Route:** `GET /api/v1/admin/clinics/{clinicId:guid}/doctors`

**Auth:** Any authenticated user

**Query Parameters:**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `pageNumber` | int | 1 | Page index |
| `pageSize` | int | 20 | Items per page (max 100) |
| `searchTerm` | string? | — | Filter by doctor name or email |
| `specializationId` | Guid? | — | Filter by specialization |

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userName": "Dr. Ahmed Ali",
      "userEmail": "ahmed@example.com",
      "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "clinicName": "عيادة السلام الطبي",
      "specializationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "specializationName": "Cardiology",
      "bio": "Senior cardiologist with 15 years experience.",
      "yearsOfExperience": 15,
      "isActive": true,
      "createdAt": "2026-07-23T00:00:00"
    }
  ],
  "totalCount": 5,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

### 1.2 `GET /api/v1/doctors/{id}` — Get doctor by ID

**Route:** `GET /api/v1/doctors/{id:guid}`

**Auth:** Any authenticated user

**Response `200 OK`:** Single `DoctorDto` (same shape as item above).

### 1.3 `POST /api/v1/admin/clinics/doctors` — Create doctor (for my clinic)

**Route:** `POST /api/v1/admin/clinics/doctors`

**Auth:** `ClinicOwner` + `ManageDoctors` permission

**Request Body:**
```json
{
  "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "specializationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "bio": "Senior cardiologist",
  "yearsOfExperience": 15
}
```

> **Note:** The `userId` must reference an existing user. Create the user first via registration or the Users API, then link them as a doctor.

**Response `201 Created`:** Returns the created `DoctorDto`.

### 1.4 `PUT /api/v1/doctors/{id}` — Update doctor

**Route:** `PUT /api/v1/doctors/{id:guid}`

**Auth:** `SuperAdmin` or `ClinicOwner`

**Request Body:**
```json
{
  "bio": "Updated bio text",
  "yearsOfExperience": 16
}
```

**Response `200 OK`:** Returns updated `DoctorDto`.

### 1.5 `DELETE /api/v1/doctors/{id}` — Delete doctor

**Route:** `DELETE /api/v1/doctors/{id:guid}`

**Auth:** `SuperAdmin` or `ClinicOwner`

**Response `200 OK`:** Returns `true`.

---

## 2. Staff Management

### 2.1 `GET /api/v1/admin/clinics/staff` — List staff (paginated)

**Route:** `GET /api/v1/admin/clinics/staff`

**Auth:** `ClinicOwner` + `ManageStaff` permission

**Query Parameters:**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `pageNumber` | int | 1 | Page index |
| `pageSize` | int | 20 | Items per page (max 100) |
| `searchTerm` | string? | — | Filter by staff name or email |
| `isActive` | bool? | — | Filter by active/inactive status |

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fullName": "Sara Mohamed",
      "email": "sara@example.com",
      "phoneNumber": "01001234567",
      "isActive": true,
      "createdAt": "2026-07-23T00:00:00"
    }
  ],
  "totalCount": 5,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

### 2.2 `POST /api/v1/admin/clinics/staff` — Create staff

**Route:** `POST /api/v1/admin/clinics/staff`

**Auth:** `ClinicOwner` + `ManageStaff` permission

Creates a new user account, assigns it to the clinic, and adds the `Staff` role.

**Request Body:**
```json
{
  "fullName": "Sara Mohamed",
  "email": "sara@example.com",
  "phoneNumber": "01001234567",
  "password": "P@ssw0rd123"
}
```

**Response `201 Created`:** Returns the new user's GUID:
```
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

### 2.3 `PUT /api/v1/admin/clinics/staff/{id}` — Update staff

**Route:** `PUT /api/v1/admin/clinics/staff/{id:guid}`

**Auth:** `ClinicOwner` + `ManageStaff` permission

**Request Body:**
```json
{
  "fullName": "Sara Ahmed Mohamed",
  "phoneNumber": "01009876543"
}
```

Both fields are optional — only provided fields are updated.

**Response `200 OK`:** Returns `true`.

### 2.4 `DELETE /api/v1/admin/clinics/staff/{id}` — Delete staff (soft delete)

**Route:** `DELETE /api/v1/admin/clinics/staff/{id:guid}`

**Auth:** `ClinicOwner` + `ManageStaff` permission

Soft-deletes the user (sets `IsDeleted = true`, `IsActive = false`).

**Response `200 OK`:** Returns `true`.

---

## 3. Error Responses

All endpoints return errors in the standard format:

```json
{
  "statusCode": 400,
  "message": "Error description in Arabic/English",
  "data": null
}
```

| Status | Meaning |
|--------|---------|
| `400` | Validation failure or bad request |
| `403` | Missing plan permission (`RequirePlanPermission`) or role (`RoleAuthorize`) |
| `404` | Resource not found |

When a plan permission is missing, the response is:
```json
{
  "statusCode": 403,
  "message": "Your current plan does not include this feature. Please upgrade to access it.",
  "data": null
}
```

---

## 4. Plan Feature Keys

The frontend reads `plan.features` from `GET /api/v1/plans` to determine which management sections to show:

| Feature Key | Sidebar Section |
|-------------|----------------|
| `doctor_management` | Shows "Doctors" management tab (ClinicOwner) |
| `staff_management` | Shows "Staff" management tab (ClinicOwner) |

---

## 5. Route Summary

| Method | Route | Auth | Permission |
|--------|-------|------|------------|
| `GET` | `/api/v1/admin/clinics/{clinicId}/doctors` | Any authenticated user | — |
| `GET` | `/api/v1/doctors/{id}` | Any authenticated user | — |
| `POST` | `/api/v1/admin/clinics/doctors` | ClinicOwner | `ManageDoctors` |
| `PUT` | `/api/v1/doctors/{id}` | SuperAdmin, ClinicOwner | — |
| `DELETE` | `/api/v1/doctors/{id}` | SuperAdmin, ClinicOwner | — |
| `GET` | `/api/v1/admin/clinics/staff` | ClinicOwner | `ManageStaff` |
| `POST` | `/api/v1/admin/clinics/staff` | ClinicOwner | `ManageStaff` |
| `PUT` | `/api/v1/admin/clinics/staff/{id}` | ClinicOwner | `ManageStaff` |
| `DELETE` | `/api/v1/admin/clinics/staff/{id}` | ClinicOwner | `ManageStaff` |
