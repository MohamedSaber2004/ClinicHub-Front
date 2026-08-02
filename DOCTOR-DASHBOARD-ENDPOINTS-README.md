# Doctor Dashboard — Backend API Integration Requirements

> **Purpose of this document:** Details all backend API endpoints, query parameters, request payloads, and response structures required to serve dynamic data for all pages in the **Doctor Dashboard** frontend.

---

## 1. Summary of Pages & Integration Status

| Page | View File | Route Path | Status | Summary |
|------|-----------|------------|--------|---------|
| **1. Overview (لوحة التحكم الرئيسية)** | `Views/Doctor/Index.cshtml` | `/Doctor` | ⚠️ Needs Endpoints | Stats KPI cards + top 5 recent appointments |
| **2. My Appointments (مواعيدي)** | `Views/Doctor/Appointments.cshtml` | `/Doctor/Appointments` | ⚠️ Needs Endpoints | Paginated appointments list + accept/reject/complete actions |
| **3. My Patients (مرضاي)** | `Views/Doctor/Patients.cshtml` | ` /Doctor/Patients` | ⚠️ Needs Endpoints | Paginated patient list + visit history sub-page |
| **4. Doctor Availability (أوقات العمل المتاحة)** | `Views/Doctor/Availability.cshtml` | `/Doctor/Availability` | ✅ Integrated | Working schedule & slot duration management |

---

## 2. Global API Conventions & Authorization

- **Base URL:** `{BaseUrl}/api/v1`
- **Authentication:** All requests must attach the HttpOnly cookie `AccessToken` via `Authorization: Bearer <token>`.
- **Doctor Identity:** The Doctor ID should be extracted automatically on the backend from the JWT claims/identity in the token context.
- **Response Envelope:**
  ```json
  {
    "isSuccess": true,
    "data": { ... },
    "errors": []
  }
  ```

---

## 3. Endpoints Specification by Page

### Page 1: Overview (`/Doctor`)

#### 1.1 `GET /doctors/dashboard/stats`
* **Description:** Fetches summary KPI statistics cards for the doctor's homepage.
* **Query Parameters:** None (Doctor ID from Bearer Token).
* **Response `data`:**
  ```json
  {
    "todayAppointmentsCount": 8,
    "totalPatientsCount": 124,
    "pendingAppointmentsCount": 3,
    "completedAppointmentsCount": 85
  }
  ```

#### 1.2 `GET /doctors/dashboard/recent-appointments`
* **Description:** Fetches the top 5 most recent appointments for the doctor overview table.
* **Query Parameters:** Optional `limit=5` (default: 5).
* **Response `data`:** Array of `DoctorAppointmentDto` (see structure under Page 2).

---

### Page 2: My Appointments (`/Doctor/Appointments`)

#### 2.1 `GET /doctors/appointments`
* **Description:** Fetches a paginated list of all appointments assigned to the doctor.
* **Query Parameters:**
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)
  - `status` (int optional filter: `0`=Pending, `1`=Confirmed, `2`=Cancelled, `3`=Completed)
  - `date` (string `YYYY-MM-DD` optional filter)
  - `searchTerm` (string optional: search by patient name or phone)
* **Response `data`:** `PagginatedResult<DoctorAppointmentDto>`
  ```json
  {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "patientId": "7cb85f64-5717-4562-b3fc-2c963f66afa7",
        "patientName": "أحمد محمود",
        "patientInitial": "أ",
        "date": "2026-08-01",
        "time": "10:30 ص",
        "specialty": "طب الأطفال",
        "status": "قيد الانتظار",
        "statusCode": 0
      }
    ],
    "totalCount": 15,
    "totalPages": 2,
    "pageNumber": 1,
    "pageSize": 10,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
  ```

#### 2.2 `PUT /doctors/appointments/{id}/status`
* **Description:** Updates the status of an appointment (e.g. Accept/Approve, Reject/Cancel, or Complete).
* **Path Parameter:** `id` (GUID)
* **Request Body:**
  ```json
  {
    "status": 1,
    "notes": "تم تأكيد الموعد"
  }
  ```
  *(Status Codes: `1`=Confirmed/Accepted, `2`=Rejected/Cancelled, `3`=Completed)*
* **Response:** Standard envelope `{ "isSuccess": true, "data": true, "errors": [] }`.

---

### Page 3: My Patients (`/Doctor/Patients`) & History

#### 3.1 `GET /doctors/patients`
* **Description:** Fetches a paginated list of registered patients associated with the doctor.
* **Query Parameters:**
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)
  - `searchTerm` (string optional)
* **Response `data`:** `PagginatedResult<DoctorPatientDto>`
  ```json
  {
    "items": [
      {
        "id": "7cb85f64-5717-4562-b3fc-2c963f66afa7",
        "name": "محمود علي",
        "initials": "م",
        "lastVisit": "2026-07-20",
        "phone": "01012345678",
        "totalVisits": 4,
        "condition": "متابعة دورية"
      }
    ],
    "totalCount": 42,
    "totalPages": 5,
    "pageNumber": 1,
    "pageSize": 10,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
  ```

#### 3.2 `GET /doctors/patients/{id}/history`
* **Description:** Fetches detailed visit history and prescriptions for a specific patient (`Views/Doctor/PatientHistory.cshtml`).
* **Path Parameter:** `id` (Patient GUID)
* **Response `data`:**
  ```json
  {
    "patientId": "7cb85f64-5717-4562-b3fc-2c963f66afa7",
    "patientName": "محمود علي",
    "history": [
      {
        "id": "8bb85f64-5717-4562-b3fc-2c963f66afa8",
        "date": "2026-07-20",
        "diagnosis": "التهاب بالحلق",
        "notes": "يحتاج راحة لمدة 3 أيام",
        "prescription": "أموكسيسيلين 500ملجم",
        "status": "مكتمل",
        "statusCode": 3
      }
    ]
  }
  ```

---

### Page 4: Doctor Availability (`/Doctor/Availability`)

#### 4.1 `GET /admin/dashboard/doctors/availability` *(✅ Currently Integrated)*
* **Description:** Loads the current weekly working schedule for the authenticated doctor.
* **Response `data`:** `List<DoctorAvailabilityDto>`
  ```json
  [
    {
      "id": "11a85f64-5717-4562-b3fc-2c963f66afa1",
      "dayOfWeek": 0,
      "startTime": "09:00:00",
      "endTime": "17:00:00",
      "slotDurationMinutes": 30
    }
  ]
  ```
  *(Days: `0`=Sun, `1`=Mon, `2`=Tue, `3`=Wed, `4`=Thu, `5`=Fri, `6`=Sat)*

#### 4.2 `PUT /admin/dashboard/doctors/availability/week` *(✅ Currently Integrated)*
* **Description:** Saves/updates weekly working slots (supports flexible per-day slot durations 1–480 minutes).
* **Request Body:**
  ```json
  {
    "days": [
      {
        "id": "11a85f64-5717-4562-b3fc-2c963f66afa1",
        "dayOfWeek": 0,
        "startTime": "09:00:00",
        "endTime": "17:00:00",
        "slotDurationMinutes": 30
      }
    ]
  }
  ```
* **Response `data`:** Returns the updated list of `DoctorAvailabilityDto` rows.
