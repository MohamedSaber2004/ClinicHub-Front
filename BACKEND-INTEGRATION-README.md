# ClinicHub — Frontend Integration README (Backend Handoff)

> **Purpose of this document:** Give the **backend project** a complete, accurate picture of the
> ClinicHub **frontend** so the backend team can implement/complete every missing API endpoint and
> integrate with the frontend **in one pass**. It documents per dashboard: (1) what is already
> implemented and successfully integrated (with the exact endpoints the frontend calls), and
> (2) what is **not** implemented and exactly what the backend must provide to wire it.
>
> Frontend repo: `E:\ClinicHub-Front\ClinicHub` — ASP.NET Core 8 MVC, Arabic RTL, design-only
> (Controllers pass data via `ViewBag` only; all HTTP calls live in `ClinicHub.Services`).

---

## 1. How the frontend consumes the API (read this first)

- **Base URL:** `{BaseUrl}/api/v1` — every path in this document is relative to `/api/v1`.
  Defined once in `ClinicHub.Services/Routes/Api/DoctoryRoutes.cs` (all route groups below).
- **Auth:** the web app stores the token in an `AccessToken` cookie (HttpOnly). Every service call
  attaches it via `BearerTokenHandler`. All admin/clinic endpoints additionally send the
  `clinic-id` header via `ClinicHeaderHandler`.
- **Localization:** every `HttpClient` sends `Accept-Language: ar` → **all error messages must be
  returned localized (Arabic by default)**. The frontend displays `message` from the error body
  as-is.
- **Response envelope (uniform):** `{ "isSuccess": bool, "data": T, "errors": [...] }`.
  HTTP status + `message` drive the UI. The frontend maps failures to `ApiException(status, message)`
  and shows the message in an error modal / banner — so the backend should return **meaningful
  localized messages** (see `ClinicHub.Services/Exceptions/ApiException.cs`).
- **Pagination:** list endpoints return `PagginatedResult<T>` →
  `{ items, totalCount, totalPages, pageNumber, pageSize, hasPreviousPage, hasNextPage }`.
  The UI renders a pagination partial that re-passes `pageNumber`, `pageSize` and current filters.
- **Frontend conventions that bind to backend behavior:**
  - Controllers never invent data — pages either render API data or fall back to mock/static.
  - Nothing is "hard-coded 30" for slot durations — always read from API (see §7).
  - `dotnet build ClinicHub.slnx` passes with 0 errors in the frontend repo.

---

## 2. Status legend

| Mark | Meaning |
|------|---------|
| ✅ **Integrated** | Page renders real data from a real backend endpoint through the service layer. |
| ⚠️ **Not implemented** | Page exists (UI built) but renders **mock/static data only** — needs backend endpoints. |
| ⛔ **Not built** | No controller action / no reachable page (dead menu link or orphan view). |

---

## 3. SuperAdmin Dashboard

Controller: `AdminController` — Views: `Views/Admin/**`. Menu lives in `_AdminLayout`.

### 3.1 ✅ Integrated (endpoints the backend must keep working)

| Page | Route | API consumed by the frontend |
|------|-------|------------------------------|
| Specializations | `/Admin/Specializations` | `GET /specializations` (pageNumber, pageSize, isFamous, isActive) · `GET /specializations/{id}` · `POST /specializations/create` · `PUT /specializations/update` · `DELETE /specializations/delete` |
| Clinics | `/Admin/Clinics` | `GET /admin/clinics/paginated` (pageNumber, pageSize, searchTerm, status, name, email, phone, createdFrom, createdTo, sortBy, sortAscending) · `GET /specializations?pageSize=200&isActive=true` · `POST /admin/clinics` · `PUT /admin/clinics/{id}` · `PATCH /admin/clinics/{id}/activate` · `PATCH /admin/clinics/{id}/deactivate` · `POST /attachments/upload` (place=5) |
| Clinic Details | `/Admin/Clinics/Details/{id}` | `GET /admin/clinics/{id}/details` (fallback `GET /admin/clinics/{id}`) · same update/activate/deactivate as above |
| Doctors | `/Admin/Doctors` | `GET /users` (userTypes=[Doctor]+ClinicOwner, clinicId, isUnassigned, searchTerm) · `GET /admin/dashboard/clinics` (lookup) · `DELETE /users/{id}` · `POST /users/change-password` · `PUT /users/{id}` |
| Verification Center | `/Admin/Verification` | `GET /admin/users/pending` · `POST /admin/users/{id}/approve` · `POST /admin/users/{id}/reject` (notes) |
| Pending Clinics | `/Admin/PendingClinics` | `GET /admin/users/pending` (filtered to ClinicOwner role) · `POST Admin/PendingClinics/Approve` (approve user + activate clinic) · `.../Reject` (reason) |
| Subscriptions (plans) | `/Admin/Subscriptions` | `GET /plans` — **read-only** plan cards |
| Subscription Management | `/Admin/SubscriptionManagement` | `GET /admin/dashboard/subscriptions` (status, planId, clinicId, paged) · `GET /admin/plans` · `POST /admin/dashboard/subscriptions/{id}/revoke` |
| Users | `/Admin/Users` | `GET /users` (paged, userTypes, searchTerm) · `GET /admin/dashboard/clinics` · `GET /specializations` · `POST /users` · `PUT /users/{id}` · `DELETE /users/{id}` · `POST /users/change-password` (userTypes flags: 2=SuperAdmin, 16=ClinicOwner, 8=Staff, 1=User) |

### 3.2 ⚠️ / ⛔ Not implemented — what the backend must add

| Page | Route | Current state | Backend endpoint(s) needed |
|------|-------|---------------|-----------------------------|
| **Dashboard (Home)** | `/Admin/Index` | ⚠️ 100% mock | `GET /admin/dashboard/stats` → overview stats; plus feeds for: doctors on duty, urgent support tickets, subscribers, activity log, new patients (the page renders lists for each). |
| **Kpi** | — | ⛔ dead redirect | Either implement a KPI page (`GET /admin/dashboard/kpi` → charts data) or remove the JS redirect. |
| **Doctor Details** | `/Admin/Doctors/Details/{id}` | ⚠️ 100% mock | `GET /admin/dashboard/doctors/{id}` → doctor profile + clinic info + clinic staff list (view renders profile card, clinic card, staff table). |
| **Payments** | `/Admin/Payments` | ⚠️ 100% mock | `GET /admin/dashboard/payments` (paged: searchTerm, status, date) + `GET /admin/dashboard/payments/stats` → stats cards. |
| **Payments Details** | `/Admin/PaymentsDetails/{id}` | ⚠️ 100% mock | `GET /admin/dashboard/payments/{id}` → payment detail + timeline (created/paid/refunded events, payer, method, amount). |
| **Support** | `/Admin/Support` | ⚠️ 100% mock | `GET /admin/support/tickets` (paged, status filter) + reply/close actions (`PUT /admin/support/tickets/{id}/reply`, `.../close`). |
| **Profile** | `/Admin/Profile` | ⚠️ hardcoded | `GET /users/me` → profile + `PUT /users/me` (name, email, phone, password change). |
| **Users sub-pages** | `/Admin/Users/Overview\|Visits\|Requests\|Payments/{id}` | ⚠️ 100% mock | Per-user: `GET /admin/dashboard/users/{id}` (profile + stats + recent activity), `.../users/{id}/visits`, `.../users/{id}/requests`, `.../users/{id}/payments`. |
| **User row metrics** (in Users list) | — | ⚠️ hardcoded (TotalVisits=0, AvgRating=0, TotalSpent=0) | The Users list response (`GET /users`) should include per-user `totalVisits`, `avgRating`, `totalSpent` (or the page needs a stats endpoint per user). |
| **Plan Management** | `/Admin/PlanManagement` | ⛔ no page | The frontend service **already implements** `POST /admin/plans`, `PUT /admin/plans/{id}`, `DELETE /admin/plans/{id}` — only the UI + controller action are missing. Add a plan CRUD page consuming these. |

---

## 4. Clinic Owner Dashboard

Controller: `ClinicController` — Views: `Views/Clinic/**`. Every action is guarded: the frontend
loads `GET /subscriptions/my` + `GET /plans` on every request and redirects to
`/Clinic/MySubscription` when the subscription expired.

### 4.1 ✅ Integrated

| Page | Route | API consumed by the frontend |
|------|-------|------------------------------|
| Staff | `/Clinic/Staff` | `GET /admin/clinics/staff` (pageNumber, pageSize, searchTerm, isActive) · `GET /admin/clinics/staff/{id}` · `POST /admin/clinics/staff` (multipart: FullName, Email, PhoneNumber, Password, ClinicId, Image) · `PUT /admin/clinics/staff/{id}` (FullName, PhoneNumber, IsActive, Image) · `PUT /admin/clinics/staff/{id}/change-password` · `DELETE /admin/clinics/staff/{id}` |
| Doctors | `/Clinic/Doctors` | `GET /specializations/active` (fallback `GET /specializations`) · `GET /admin/clinics/{clinicId}/doctors` (paged, searchTerm, specializationId) · `GET /doctors/{id}` · `POST /admin/clinics/doctors` (JSON: user fields + `availabilities[]` with per-row `slotDurationMinutes`) · `PUT /doctors/{id}` · `PUT /admin/clinics/doctors/{id}/change-password` · `DELETE /doctors/{id}` |
| Online Booking | `/Clinic/OnlineBooking` | `GET /admin/clinics/settings` (MaxAdvanceBookingDays) · `GET /admin/clinics/{clinicId}/doctors?pageSize=100` · `GET /admin/dashboard/clinics/{clinicId}/doctors/{doctorId}/slots?date=YYYY-MM-DD` → `data.days[]` (one entry per availability row: dayOfWeek, workingHours{from,to}, slotDurationMinutes, slots[{id,startTime,endTime,isAvailable}]) · `POST /admin/dashboard/appointments` with `{clinicId, doctorId, date, startTime, endTime, patientName, patientPhone}` (must be exactly a returned slot; errors: `Booking.InvalidDate` 400, `AppointmentMessages.DoctorNotAvailableAtThisTime` 400) |
| Settings | `/Clinic/Settings` | `GET /admin/clinics/settings` · `GET /specializations/active` · `PUT /admin/clinics/settings` (UpdateClinicSettingsRequest) · also computes **typical slot duration** from the clinic doctors list |
| My Subscription | `/Clinic/MySubscription` | `GET /subscriptions/my` · `GET /plans` · `POST /subscriptions/my/cancel` |
| Subscribe | `/Clinic/Subscribe?planId&period` | `POST /subscriptions/initiate-payment` → redirects to `TargetRedirectUrl` |
| InitiatePayment | `/Clinic/InitiatePayment` | `POST /subscriptions/initiate-payment` |

### 4.2 ⚠️ / ⛔ Not implemented — what the backend must add

| Page | Route | Current state | Backend endpoint(s) needed |
|------|-------|---------------|-----------------------------|
| **Dashboard (Home)** | `/Clinic/Index` | ⚠️ stats hardcoded (45 visits, 6 clinics, 14 waiting, 28 cases) + hardcoded tables | `GET /clinics/{clinicId}/dashboard/stats` → today's appointments, total patients, waiting count, case statuses + recent activity/appointments lists. |
| **Appointments** | `/Clinic/Appointments` | ⚠️ static calendar + SMTP reminder tabs | `GET /clinics/{clinicId}/appointments` (calendar view: date range, status) + reminder settings (`GET/PUT /clinics/{clinicId}/reminders`). |
| **Appointment Revenue** | `/Clinic/AppointmentRevenue` | ⚠️ 100% mock (`MockAppointmentPayment`) | `GET /clinics/{clinicId}/revenue` → per-appointment revenue list (date, patient, doctor, amount, status) + totals. (Invoice stats endpoint already exists — see §9.) |
| **Medical Records (EMR)** | `/Clinic/MedicalRecords` | ⚠️ static hardcoded patient rows | `GET /clinics/{clinicId}/patients` (searchable) + `GET/POST /patients/{id}/records` (medical records CRUD). |
| **Inventory** | `/Clinic/Inventory` | ⚠️ static hardcoded items + low-stock alert | `GET /clinics/{clinicId}/inventory` (paged, search) + `POST/PUT/DELETE /clinics/{clinicId}/inventory/{id}` (item: name, category, quantity, unit, minThreshold). |
| **Patient Portal** | `/Clinic/PatientPortal` | ⚠️ static hardcoded chats | `GET /clinics/{clinicId}/patients` (list) + messaging endpoints (`GET/POST /clinics/{clinicId}/messages`). |
| **Reports** | `/Clinic/Reports` | ⚠️ stub "سيتم إضافة محتوى صفحة التقارير قريباً" | Reporting endpoints (revenue over time, appointment stats, doctor performance) or the page stays a stub. |
| **Marketing** | `/Clinic/Marketing` | ⚠️ stub | Marketing/campaign endpoints (or stub stays). |
| **Support** | `/Clinic/Support` | ⚠️ stub | `GET /clinics/{clinicId}/support/tickets` + `POST .../tickets` + `PUT .../tickets/{id}/reply`. |
| **Billing / Invoices** | `/Clinic/Billing`, `/Clinic/InvoiceCreate?id=`, `/Clinic/InvoiceDetail/{id}` | ⛔ **No action, no view — but the service layer is 100% ready** | See §9 — the full invoice API is already consumed by `InvoiceService` and documented in `docs/invoices-api.md`; the backend must implement those endpoints and the frontend team will build the 3 pages (Billing list, invoice create/edit, invoice detail). |
| **PaymentFrame** | — | ⛔ orphan iframe view | Payment-iframe page for the paymob redirect (needs an action that receives the payment URL and renders the iframe watching `result.html?success=`). |

---

## 5. Doctor Dashboard

Controller: `DoctorController` — Views: `Views/Doctor/**`. The doctor identity should be resolved from the authenticated user token (`BearerTokenHandler`).

### 5.1 Overview of Pages & Integration Status

| Page | View | Route | Status | Notes |
|------|------|-------|--------|-------|
| **1. Overview (الصفحة الرئيسية)** | `Index.cshtml` | `/Doctor` | ⚠️ 100% Mock | Needs stats cards + recent appointments list |
| **2. My Appointments (مواعيدي)** | `Appointments.cshtml` | `/Doctor/Appointments` | ⚠️ 100% Mock | Needs paginated appointments list + status update actions |
| **3. My Patients (مرضاي)** | `Patients.cshtml` | `/Doctor/Patients` | ⚠️ 100% Mock | Needs paginated patients list + patient history endpoint |
| **4. Doctor Availability (أوقات العمل المتاحة)** | `Availability.cshtml` | `/Doctor/Availability` | ✅ Integrated | Consumes `GET` & `PUT` availability endpoints |

---

### 5.2 Required Endpoints Breakdown for Backend Team

Below is the detailed specification of the endpoints required by the Backend to provide dynamic data for all pages in the Doctor Dashboard:

#### Page 1: Overview (`/Doctor`)
* **`GET /doctors/dashboard/stats`** (or `/admin/dashboard/doctors/stats`)
  - **Purpose:** Fetches top summary KPI stat cards for the doctor.
  - **Query Parameters:** None (Doctor ID extracted from JWT Token).
  - **Response `Data` Structure:**
    ```json
    {
      "todayAppointmentsCount": 8,
      "totalPatientsCount": 124,
      "pendingAppointmentsCount": 3,
      "completedAppointmentsCount": 85
    }
    ```
* **`GET /doctors/dashboard/recent-appointments`** (or query `GET /doctors/appointments?pageSize=5&sortBy=Date&sortAscending=false`)
  - **Purpose:** Fetches the top 5 most recent appointments for the overview table.
  - **Response `Data` Structure:** List of Appointment DTOs (see structure in Page 2 below).

---

#### Page 2: My Appointments (`/Doctor/Appointments`)
* **`GET /doctors/appointments`**
  - **Purpose:** Fetches a paginated list of all appointments for the logged-in doctor.
  - **Query Parameters:** `pageNumber` (int), `pageSize` (int), `status` (int/string optional filter: 0=Pending, 1=Confirmed, 2=Cancelled, 3=Completed), `date` (YYYY-MM-DD optional filter), `searchTerm` (string patient name/phone).
  - **Response `Data` Structure:** `PagginatedResult<DoctorAppointmentDto>`
    ```json
    {
      "items": [
        {
          "id": "guid",
          "patientId": "guid",
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
* **`PUT /doctors/appointments/{id}/status`** (or `/doctors/appointments/{id}/approve`, `/complete`, `/reject`)
  - **Purpose:** Accepts, rejects, or marks an appointment as completed.
  - **Request Body:** `{ "status": 1, "notes": "optional" }` (Status: 1=Confirmed/Accepted, 2=Rejected/Cancelled, 3=Completed).
  - **Response:** Standard envelope `{ "isSuccess": true, "data": true, "errors": [] }`.

---

#### Page 3: My Patients (`/Doctor/Patients`) & Medical History
* **`GET /doctors/patients`**
  - **Purpose:** Fetches a paginated list of registered patients associated with this doctor.
  - **Query Parameters:** `pageNumber` (int), `pageSize` (int), `searchTerm` (string).
  - **Response `Data` Structure:** `PagginatedResult<DoctorPatientDto>`
    ```json
    {
      "items": [
        {
          "id": "guid",
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
      "pageSize": 10
    }
    ```
* **`GET /doctors/patients/{id}/history`** (Used by sub-page `/Doctor/PatientHistory/{id}`)
  - **Purpose:** Fetches detailed visit and medical history for a specific patient.
  - **Response `Data` Structure:**
    ```json
    {
      "patientId": "guid",
      "patientName": "محمود علي",
      "history": [
        {
          "id": "guid",
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

#### Page 4: Doctor Availability (`/Doctor/Availability`)
* **`GET /admin/dashboard/doctors/availability`**
  - **Status:** ✅ Implemented & Integrated
  - **Purpose:** Loads current weekly working schedule for the authenticated doctor.
  - **Response `Data` Structure:** `List<DoctorAvailabilityDto>`
    ```json
    [
      {
        "id": "guid",
        "dayOfWeek": 0,
        "startTime": "09:00:00",
        "endTime": "17:00:00",
        "slotDurationMinutes": 30
      }
    ]
    ```
* **`PUT /admin/dashboard/doctors/availability/week`**
  - **Status:** ✅ Implemented & Integrated
  - **Purpose:** Saves/updates weekly schedule rows (handles slot duration per day 1–480 min).
  - **Request Body:**
    ```json
    {
      "days": [
        {
          "id": "guid (optional for existing)",
          "dayOfWeek": 0,
          "startTime": "09:00:00",
          "endTime": "17:00:00",
          "slotDurationMinutes": 30
        }
      ]
    }
    ```
  - **Response:** Standard envelope returning the updated list of availability rows.

---

## 6. Staff Dashboard — reference implementation (100% integrated)

Controller: `StaffController` — Views: `Views/Staff/**`. **This dashboard is the reference pattern**
for wiring the rest: every page consumes a real endpoint via `IStaffDashboardService`.

| Page | Route | API consumed by the frontend |
|------|-------|------------------------------|
| Home | `/Staff/Index` | `GET /staff/dashboard/stats` (StaffDashboardStatsDto) · `GET /staff/queue` (top 5) |
| Appointments | `/Staff/Appointments` | `GET /staff/appointments` (pageNumber, pageSize, status, date, patientName) |
| Queue | `/Staff/Queue` | `GET /staff/queue` · `GET /staff/doctors` |
| Register Patient | `/Staff/RegisterPatient` | `GET /staff/doctors` · `POST /staff/patients/register` (fullName, phoneNumber, doctorId, complaint, appointmentDate, startTime, endTime, appointmentType=0, clinicId) |
| Doctor Schedule | `/Staff/DoctorSchedule/{doctorId}` | `GET /staff/doctors/{doctorId}/schedule?date=YYYY-MM-DD` |
| Action endpoints | — | `PUT /staff/appointments/{id}/approve` · `PUT /staff/appointments/{id}/reject` (body `{reason}`) · `PUT /staff/appointments/{id}/check-in` · `PUT /staff/appointments/{id}/complete` |

**Appointment status ints (backend → UI mapping):** 0=pending, 1=confirmed, 2=cancelled, 3=completed,
4=reserved, 5=noshow, 6=accepted, 7=rejected. Queue statuses: registered/waiting/in-progress/completed.

---

## 7. Public & Auth pages

| Page | Status | Notes |
|------|--------|-------|
| Login / Logout / Refresh / Forgot+Reset password | ✅ | `POST /auth/login-web` (sets token cookies; redirects by role: SuperAdmin→/Admin, ClinicOwner→/Clinic, Staff→/Staff, Doctor→/Doctor), `POST /auth/forget-password`, `POST /auth/verify-reset-token`, `POST /auth/reset-password`, `POST /auth/refresh-token`, `POST /auth/logout` |
| Subscriptions | ✅ | `GET /plans` (PriceMonthly/PriceYearly) |
| Clinic Register | ✅ | `GET /plans`, `GET /specializations/active`, `POST /clinics/register` + `POST /attachments/upload` (place=5, images) |
| Home marketing pages | ⚠️ | static, no API |
| PaymentResult | ⚠️ | static success/failure screen |
| VerificationApproved | ⚠️ | stub — **no email-verification API exists**; the page expects a link with `token` + `status=accepted` + a verification endpoint |
| PendingApproval / SubscriptionRequired / RegistrationSubmitted | ⚠️ | static status pages (no API needed) |

---

## 8. Route discrepancies to resolve (frontend vs backend docs)

The frontend **actually calls** the routes below (from `DoctoryRoutes.cs`), but existing backend docs
in this repo describe different paths. Align the backend to the frontend:

| Frontend calls (authoritative) | Docs say | Notes |
|-------------------------------|----------|-------|
| `GET /admin/dashboard/clinics/{clinicId}/doctors/{doctorId}/slots?date=` | `GET /clinics/{clinicId}/doctors/{doctorId}/slots` (docs/dynamic-slot-duration.md) | Keep the frontend route; update the doc. |
| `POST /admin/dashboard/appointments` | `POST /appointments` or `/reservations` | Frontend posts `BookAppointmentRequest` here. |
| `GET/PUT /admin/dashboard/doctors/availability(/week)` | `GET/PUT /doctors/availability(/week)` (docs/doctor-availability-api.md) | Frontend calls the `/admin/dashboard` prefixed path. |

---

## 9. Backend endpoints the frontend ALREADY consumes but may not exist yet

These are called by `ClinicHub.Services` today (compiled, 0 errors). Implement them 1:1 with the
fields below and every page they serve works immediately:

- **Invoices** (`InvoiceService` — UI pages ⛔ but service wired; contract in `docs/invoices-api.md`):
  `GET /clinics/{clinicId}/invoices` (pageNumber, pageSize, status, fromDate, toDate, patientId) ·
  `GET /clinics/{clinicId}/invoices/{invoiceId}` · `GET /clinics/{clinicId}/invoices/stats` ·
  `POST /clinics/{clinicId}/invoices` (create draft: patientId?, items[{description, quantity, unitPrice, discount}], discountType 0=Percentage/1=Fixed, discountValue, taxRate) ·
  `PUT /clinics/{clinicId}/invoices/{invoiceId}` (update draft) ·
  `POST /clinics/{clinicId}/invoices/{invoiceId}/issue` · `.../cancel` (body {reason}) ·
  `POST /payments` (invoiceId, amount, method 0=Cash/1=Card/2=Wallet, transactionRef required for Card/Wallet, notes).
  Invoice DTO fields the UI expects: `InvoiceNumber` (auto `INV-{Year}-{Seq:0004}` on issue), `SubTotal`, `DiscountType/Value`, `TaxRate/Amount`, `Total`, `LineItems`, `PaymentSettlements`, `CancellationReason`, `CreatedAt/IssuedAt/PaidAt/CancelledAt`, status 0=Draft/1=Issued/2=Paid/3=Cancelled/4=Refunded.
- **Admin plan CRUD:** `POST /admin/plans` · `PUT /admin/plans/{id}` · `DELETE /admin/plans/{id}` (service ready; UI not built — see §3.2 Plan Management).
- **Attachments:** `PUT /attachments/update/{name}` · `POST /attachments/upload-multiple-attachments` · `GET /attachments/download?place&fileName`.
- **Doctor availability by id:** `GET /admin/dashboard/doctors/availability/{id}`.
- **Admin pending clinics:** `GET /admin/dashboard/clinics/pending` (currently the page reuses `/admin/users/pending` instead).

---

## 10. Backend one-time implementation checklist (priority order)

Do these in this order — each unblocks whole pages:

1. **Staff dashboard** (if not done): §6 — small, completes a full dashboard.
2. **Admin dashboard home + doctor details + user sub-pages** (§3.2): dashboard stats, doctor detail, per-user overview/visits/requests/payments, user row metrics in `GET /users`.
3. **Doctor dashboard data** (§5.2): stats, appointments, patients, patient history.
4. **Clinic dashboard core** (§4.2): dashboard stats, appointments calendar, revenue.
5. **Invoice endpoints** (§9): the frontend service is ready — then build Billing/InvoiceCreate/InvoiceDetail pages.
6. **Admin payments + support + profile** (§3.2) and **clinic support/marketing/reports** (§4.2).
7. **Clinic medical records, inventory, patient portal** (§4.2).
8. **Admin plan management UI** (endpoints ready) + **email verification flow** (new API).
9. **Resolve route discrepancies** (§8) — align backend to the frontend's `/admin/dashboard/*` prefix.

---

## 11. Related contract docs (in the frontend repo `docs/`)

| Doc | Covers |
|-----|--------|
| `docs/dynamic-slot-duration.md` | Per-row slot duration: slots endpoint shape, booking-window rule, per-consumer endpoints |
| `docs/doctor-availability-api.md` | Doctor weekly availability load/save contract |
| `docs/create-doctor-with-availability.md` | Creating a doctor with availability rows (clinic owner flow) |
| `docs/update-and-get-doctor.md` | Doctor update/get endpoints |
| `docs/clinic-settings-api.md` / `clinic-settings-integration.md` | Clinic settings GET/PUT, maxAdvanceBookingDays |
| `docs/invoices-api.md` + `docs/INVOICES-PAYMENTS-PLAN.md` | Full invoice & payment lifecycle |
| `docs/clinic-details.md` | Clinic details endpoint |
| `docs/IMAGE-INTEGRATION.md` | Attachment upload/download and image URLs |
