# Clinic Dashboard — API Endpoints Reference

> Use this document to verify authorization/permission checks on every backend endpoint consumed by the clinic dashboard frontend.

---

## Base URL

```
{baseUrl}/api/v1
```

---

## Headers Sent by Default

| Header | Source | When |
|--------|--------|------|
| `Authorization: Bearer {token}` | `BearerTokenHandler` — reads from cookie `AccessToken` or `Authorization` header | All requests **except** anonymous auth paths (login, refresh-token, forget-password, reset-password, specializations/active, clinics/register, attachments/upload). |
| `X-ClinicId: {guid}` | `ClinicHeaderHandler` — reads from `HttpContext.Items["ClinicId"]` | Always (if set), but currently `Items["ClinicId"]` is **never populated** — this header is never actually sent. |
| `Accept-Language: ar` | Set globally on typed `HttpClient` instances | All requests |

---

## 1. Subscription Endpoints

### GET `/subscriptions/my`

**Called from:** Every clinic dashboard page (via `OnActionExecutionAsync` filter) + `MySubscription()` page  
**Auth:** Bearer token required  
**Permission:** User must be authenticated and have an active subscription  
**Purpose:** Fetches the current user's subscription (plan, clinicId, status, dates). Used to:
- Derive `ClinicId` for the controller's `CurrentUserContext`
- Check if plan is expired → redirect to subscription page
- Show plan name in sidebar

**Response shape:** `SubscriptionDto` — `Id`, `ClinicId`, `ClinicName`, `PlanId`, `PlanName`, `Period`, `StartDate`, `EndDate`, `Status`, `Amount`, `PaidAt`, `IsActive`

---

### POST `/subscriptions/initiate-payment`

**Called from:** `Subscribe()` + `InitiatePayment()`  
**Auth:** Bearer token required  
**Permission:** Authenticated user  
**Request body:**
```json
{
  "planId": "guid",
  "period": 0,
  "returnUrl": "string"
}
```
**Purpose:** Initiates payment flow for a plan subscription. Returns a redirect URL to the payment gateway.

**Response shape:** `{ targetRedirectUrl: string }`

---

### POST `/subscriptions/my/cancel`

**Called from:** `CancelSubscription()`  
**Auth:** Bearer token required  
**Permission:** Authenticated user — must own the subscription  
**Purpose:** Cancels the current user's subscription.

---

## 2. Plan Endpoints

### GET `/plans`

**Called from:** Every clinic dashboard page (via `OnActionExecutionAsync` filter) + `MySubscription()` + `Subscribe()`  
**Auth:** Bearer token optional (public-friendly endpoint — `_publicEndpoints` list allows no-token access)  
**Permission:** None required (or any authenticated user)  
**Purpose:** Fetches all available plans (pricing, features, max doctors/staff). Used to:
- Derive `MaxDoctors`, `MaxStaff`, `PlanFeatures` for the sidebar and plan-info bars
- Show plan comparison on subscription page

**Response shape:** Array of `PlanDto` — `Id`, `Name`, `NameAr`, `Description`, `DescriptionAr`, `PriceMonthly`, `PriceYearly`, `MaxDoctors`, `MaxStaff`, `Features` (JSON array of strings), `IsActive`, `SortOrder`

---

## 3. Doctor Endpoints

### GET `/admin/clinics/{clinicId}/doctors?pageNumber={n}&pageSize={n}&searchTerm={string}&specializationId={guid}`

**Called from:** `Doctors()` — list page with search/filter  
**Auth:** Bearer token required  
**Permission:** User must have `ManageClinicDoctors` permission AND `ManageDoctors` plan feature  
**Purpose:** Fetches paginated list of doctors for a specific clinic.

**Query parameters:** `pageNumber` (default 1), `pageSize` (default 20), `searchTerm` (optional), `specializationId` (optional GUID)

**Response shape:** `PagginatedResult<DoctorDto>` — `Items: [{ Id, UserId, UserName, UserEmail, ClinicId, ClinicName, SpecializationId, SpecializationName, Bio, YearsOfExperience, IsActive, CreatedAt }]`, `TotalCount`, `PageNumber`, `PageSize`, `TotalPages`, `HasPreviousPage`, `HasNextPage`

---

### POST `/admin/clinics/doctors`

**Called from:** `CreateDoctor()` — add new doctor form  
**Auth:** Bearer token required  
**Permission:** User must have `ManageClinicDoctors` permission AND `ManageDoctors` plan feature  
**Purpose:** Creates a new doctor record that links an existing user to a clinic.

**Request body:**
```json
{
  "clinicId": "guid",
  "userId": "guid",
  "specializationId": "guid",
  "bio": "string (optional)",
  "yearsOfExperience": 0
}
```

**Note:** The frontend first creates a user (POST `/users`, see section 6), then calls this endpoint with the returned `userId`.

**Response shape:** `DoctorDto`

---

### PUT `/doctors/{id}`

**Called from:** `UpdateDoctor()` — edit doctor modal  
**Auth:** Bearer token required  
**Permission:** User must have `ManageClinicDoctors` permission AND `ManageDoctors` plan feature. Doctor must belong to user's clinic.  
**Purpose:** Updates doctor's bio and years of experience.

**Request body:**
```json
{
  "bio": "string (optional)",
  "yearsOfExperience": 0
}
```

**Response shape:** `DoctorDto`

---

### DELETE `/doctors/{id}`

**Called from:** `DeleteDoctor()` — delete doctor button  
**Auth:** Bearer token required  
**Permission:** User must have `ManageClinicDoctors` permission AND `ManageDoctors` plan feature. Doctor must belong to user's clinic.  
**Purpose:** Deletes a doctor record.

**Response shape:** `true` (boolean)

---

## 4. Staff Endpoints

### GET `/admin/clinics/staff?pageNumber={n}&pageSize={n}&searchTerm={string}&isActive={bool}`

**Called from:** `Staff()` — list page with search/filter  
**Auth:** Bearer token required  
**Permission:** User must have `ManageClinicStaff` permission AND `ManageStaff` plan feature  
**Purpose:** Fetches paginated list of staff members for the user's clinic.  
**Note:** No `clinicId` is sent as a parameter — backend must derive the clinic from the authenticated user or use a header. The `X-ClinicId` header is **not currently sent** (see headers section).

**Query parameters:** `pageNumber` (default 1), `pageSize` (default 20), `searchTerm` (optional), `isActive` (optional bool)

**Response shape:** `PagginatedResult<StaffDto>` — `Items: [{ Id, FullName, Email, PhoneNumber, IsActive, CreatedAt }]`, pagination fields

---

### POST `/admin/clinics/staff`

**Called from:** `CreateStaff()` — add new staff form  
**Auth:** Bearer token required  
**Permission:** User must have `ManageClinicStaff` permission AND `ManageStaff` plan feature  
**Purpose:** Creates a new staff member.

**Request body:**
```json
{
  "fullName": "string",
  "email": "string",
  "phoneNumber": "string",
  "password": "string",
  "clinicId": "guid"
}
```

**Response shape:** GUID (the new staff/ user ID)

---

### PUT `/admin/clinics/staff/{id}`

**Called from:** `UpdateStaff()` — edit staff modal  
**Auth:** Bearer token required  
**Permission:** User must have `ManageClinicStaff` permission AND `ManageStaff` plan feature. Staff must belong to user's clinic.  
**Purpose:** Updates staff member's name and phone number.

**Request body:**
```json
{
  "fullName": "string",
  "phoneNumber": "string (optional)"
}
```

**Response shape:** `true` (boolean)

---

### DELETE `/admin/clinics/staff/{id}`

**Called from:** `DeleteStaff()` — delete staff button  
**Auth:** Bearer token required  
**Permission:** User must have `ManageClinicStaff` permission AND `ManageStaff` plan feature. Staff must belong to user's clinic.  
**Purpose:** Deletes a staff member.

**Response shape:** `true` (boolean)

---

## 5. Specialization Endpoints

### GET `/specializations?PageNumber={n}&PageSize={n}&IsActive={bool}&IsFamous={bool}`

**Called from:** `Doctors()` — primary attempt to load specialization dropdown  
**Auth:** Bearer token optional (public-friendly — `_publicEndpoints` list)  
**Permission:** None required  
**Purpose:** Fetches paginated list of all specializations. Fallback: `GET /specializations/active?isFamous={bool}` (anonymous, no auth).

**Query parameters:** `PageNumber` (default 1), `PageSize` (default 200), `IsActive` (optional bool), `IsFamous` (optional bool)

**Response shape:** `PagginatedResult<SpecializationDto>` — `Items: [{ Id, ArName, EnName, IsActive, IsFamous, Icon }]`

---

### GET `/specializations/active?isFamous={bool}`

**Called from:** `Doctors()` — fallback when primary auth call fails  
**Auth:** **None** (anonymous — in `_neverSendTokenPaths` list)  
**Permission:** None required  
**Purpose:** Fetches active specializations only (no pagination).

**Response shape:** Array of `SpecializationDto`

---

## 6. User Endpoints

### GET `/users?PageNumber={n}&PageSize={n}&SearchTerm={string}&IsUnassigned={bool}`

**Called from:** `SearchUsers()` — user search for doctor creation  
**Auth:** Bearer token required  
**Permission:** Authenticated user with clinic association  
**Purpose:** Searches for users to link as doctors. The `IsUnassigned` flag filters to users not already linked to a doctor.

**Query parameters:** `PageNumber` (default 1), `PageSize` (default 20), `SearchTerm` (optional), `IsUnassigned` (optional bool), `UserTypes` (optional list), `ClinicId` (optional), `UserId` (optional)

**Response shape:** `PagginatedResult<UserResponseDto>` — `Items: [{ Id, FullName, Email, PhoneNumber, BirthDate, Gender, IsActive, Roles, CreatedAt }]`

---

### POST `/users`

**Called from:** `CreateDoctor()` — creates a user before linking as doctor  
**Auth:** Bearer token required  
**Permission:** Authenticated user with clinic association  
**Purpose:** Creates a new user with role `Doctor` and all doctor-specific fields in a single call.

**Request body:**
```json
{
  "fullName": "string",
  "email": "string",
  "password": "string",
  "phoneNumber": "string",
  "role": "Doctor",
  "clinicId": "guid",
  "specializationId": "guid",
  "bio": "string (optional)",
  "yearsOfExperience": 0
}
```

**Response shape:** `{ data: guid (user ID), success: bool, message: string }`

---

## Permission Summary by Endpoint

| # | Endpoint | HTTP | Frontend Gate (UI) | Required Backend Permission/Check |
|---|----------|------|--------------------|-------------------------------------|
| 1 | `/subscriptions/my` | GET | — (runs on every page) | Authenticated user |
| 2 | `/subscriptions/initiate-payment` | POST | Subscription page | Authenticated user |
| 3 | `/subscriptions/my/cancel` | POST | Cancel button | Authenticated user, owns subscription |
| 4 | `/plans` | GET | — (runs on every page) | None (public-friendly) |
| 5 | `/admin/clinics/{clinicId}/doctors` | GET | `ManageClinicDoctors` + `ManageDoctors` feature | `ManageClinicDoctors`, clinic ownership |
| 6 | `/admin/clinics/doctors` | POST | `ManageClinicDoctors` + `ManageDoctors` feature | `ManageClinicDoctors`, clinic ownership |
| 7 | `/doctors/{id}` | PUT | `ManageClinicDoctors` + `ManageDoctors` feature | `ManageClinicDoctors`, doctor belongs to clinic |
| 8 | `/doctors/{id}` | DELETE | `ManageClinicDoctors` + `ManageDoctors` feature | `ManageClinicDoctors`, doctor belongs to clinic |
| 9 | `/admin/clinics/staff` | GET | `ManageClinicStaff` + `ManageStaff` feature | `ManageClinicStaff`, clinic ownership |
| 10 | `/admin/clinics/staff` | POST | `ManageClinicStaff` + `ManageStaff` feature | `ManageClinicStaff`, clinic ownership |
| 11 | `/admin/clinics/staff/{id}` | PUT | `ManageClinicStaff` + `ManageStaff` feature | `ManageClinicStaff`, staff belongs to clinic |
| 12 | `/admin/clinics/staff/{id}` | DELETE | `ManageClinicStaff` + `ManageStaff` feature | `ManageClinicStaff`, staff belongs to clinic |
| 13 | `/specializations` | GET | — (public list) | None (public-friendly) |
| 14 | `/specializations/active` | GET | — (public list) | None (anonymous) |
| 15 | `/users` | GET | — (internal search) | Authenticated user with clinic |
| 16 | `/users` | POST | — (internal) | Authenticated user with clinic |

---

## Known Issues / Notes

1. **`X-ClinicId` header is never sent.** `ClinicHeaderHandler` reads from `HttpContext.Items["ClinicId"]`, but no code in `OnActionExecutionAsync` or elsewhere sets `Items["ClinicId"]`. The `ClinicId` is only stored in `CurrentUserContext.ClinicId` (a controller property). The header is effectively dead code. Backend should either:
   - Derive clinic from the authenticated user, OR
   - Read `clinicId` from the request body (already sent for create endpoints), OR
   - Frontend needs to set `HttpContext.Items["ClinicId"]` somewhere.

2. **Staff list (`GET /admin/clinics/staff`) sends no clinic identifier.** Unlike doctors (which have `{clinicId}` in the URL), the staff list endpoint has no clinicId parameter. With the `X-ClinicId` header not working, backend cannot determine which clinic's staff to return. Backend should either:
   - Accept `clinicId` query parameter, OR
   - Derive clinic from authenticated user's subscription, OR
   - Fix the `X-ClinicId` header pipeline.

3. **Doctor create flow is two-step:** First `POST /users` (to create user with Role=Doctor), then `POST /admin/clinics/doctors` (to link user as doctor). Both must succeed; the frontend treats the first step's failure as a hard error.

4. **UI-side permission gating** uses `Permission.ManageClinicDoctors`/`Permission.ManageClinicStaff` + `PlanFeature.ManageDoctors`/`PlanFeature.ManageStaff`. Backend should mirror these checks.