---
name: execution
description: Use when the user requests actual code execution — implementing a plan, writing code, building views, applying a feature step. Triggered by keywords like "نفّذ", "اكتب الكود", "طبّق", "implement", "execute", "start implementation". Do NOT use for planning, discussion, or code review.
---

# Execution Skill — ClinicHub

## Objective
Execute code in a structured, step-by-step manner. Every implementation consumes real backend API endpoints through service layer contracts. Follow the project's established patterns: ViewBag-only data flow, design tokens, Arabic RTL UI layouts.

## Project Architecture

```
ASP.NET Core 8 MVC (RTL/Arabic)
├── Controllers/ — Pass data via ViewBag only (no @model directives)
│   ├── BaseController — abstract base: CurrentUser, IsAjaxRequest, RedirectJson(), Fail(), LoadHeaderProfileAsync()
│   ├── HomeController — public pages (subscriptions, clinic registration, attachments)
│   ├── AccountController — auth (login, forgot/reset password, verification)
│   ├── AdminController — system admin (clinics, doctors, specializations, payments, users, support)
│   ├── ClinicController — clinic owner/manager (appointments, doctors, staff, settings)
│   ├── DoctorController — doctor panel (appointments, patients, history)
│   └── StaffController — reception/front-desk (queue, appointments, register patient)
├── Services/Contracts/ — Service interfaces (IAuthService, IClinicService, IDoctorService, etc.)
├── Services/ReponseModels/ — API response DTOs (ApiResponse<T>, PagginatedResult<T>, etc.)
├── Services/RequestModels/ — API request DTOs (LoginRequest, RegisterClinicRequest, etc.)
├── Services/Exceptions/ — ApiException, AuthenticatedApiException
├── Data/
│   ├── Roles.cs — Flag enums: Permission, PlanFeature, UserRole, ClinicStaffRole
│   └── CurrentUserContext.cs — User context DTO with HasPermission/HasFeature
├── Routes/ — Static route helpers per area (AdminRoutes.Pages.Index(), etc.)
├── Views/Shared/ — Layouts + partials (_Pagination, _ProfilePage, _SuccessModal, _ErrorModal, _ConfirmModal)
└── wwwroot/css/
    ├── design-system.css — Design tokens + utility classes
    └── site.css — Layout styles (sidebar, tables, cards, grid)
```

## Integration Layer (Service Contracts)

All controllers consume backend API through typed service interfaces injected via DI:

### Available Services
| Interface | Purpose | Used By |
|-----------|---------|---------|
| `IAuthService` | Login, refresh, logout, my-profile (`GET /auth/profile` → `GetProfileAsync()`, `PATCH /auth/profile/update` → `UpdateProfileAsync()`) | AccountController + Admin/Clinic/Doctor/Staff (profile) |
| `IPlanService` | Subscription plans CRUD | HomeController, AdminController, ClinicController |
| `ISubscriptionService` | Clinic subscriptions, registration | HomeController, ClinicController |
| `IAdminSubscriptionService` | Admin subscription management — list + revoke + plans CRUD + **create subscription** (`CreateSubscriptionAsync` → `POST /api/v1/admin/dashboard/subscriptions` — see `docs/superadmin-create-subscription-api.md` for the backend contract) | AdminController || `ISpecializationService` | Medical specializations CRUD | HomeController, AdminController, ClinicController |
| `IClinicService` | Clinic management (CRUD, activate/deactivate, details) | AdminController |
| `IClinicDoctorService` | Clinic-specific doctor management (create with availability); also patient-booking slots fetch + appointment booking (`GetAvailableSlotsAsync(clinicId, doctorId, date)`, `BookAppointmentAsync(request)`) | ClinicController |
| `IClinicStaffService` | Clinic-specific staff management | ClinicController |
| `IDoctorService` | Doctor management (admin) + own availability load/save (week replace) | AdminController, DoctorController |
| `IUserService` | User management, search (SearchUsers with IsUnassigned flag) | AdminController, ClinicController |
| `IUserVerificationService` | Verification requests management | AdminController |
| `IStaffDashboardService` | Staff dashboard (stats, queue, appointments) | StaffController |
| `IAdminPaymentService` | Admin payments page: list + filters (`GetPaymentsAsync`), detail (`GetPaymentDetailAsync`), monthly stats (`GetPaymentStatsAsync(DateTime? fromDate, DateTime? toDate)` — frontend always sends the selected month range; backend contract in `docs/superadmin-monthly-stats-api.md`), manual payment (`CreateManualPaymentAsync`), refund (`RefundPaymentAsync`), admin ads order creation (`GetEligibleClinicsAsync`, `GetAdPackagesAsync`, `CreateAdsOrderAsync`) | AdminController |
| `IAdService` | Ads feature (full spec `docs/superadmin-ads-feature.md`, backend contract `docs/ads-backend-contract.md` — endpoints MISSING on backend): clinic side — `GetMyAdsAsync(clinicId, status)`, `GetPackagesAsync()` (active only), `CreateOrderAsync(clinicId, CreateAdsOrderRequest)` → Paymob URL; admin side — `GetAdsAsync(page, size, status)` (paginated `PagginatedResult<AdDto>`), `DeactivateAdAsync(id, reason)`, `GetAllPackagesAsync()`, `CreatePackageAsync`/`UpdatePackageAsync`/`DeletePackageAsync(UpsertAdPackageRequest)`. Registered with BearerToken + ClinicHeader handlers (used by both AdminController and ClinicController) | AdminController, ClinicController |
| `IAttachmentService` | File uploads | HomeController, AdminController, ClinicController, DoctorController, StaffController |
| `IAttachmentUrlResolver` | Resolve attachment URLs (full URL from relative path) | AdminController, all dashboard layouts |
| `IRatingsService` | Ratings read-only (display): `GetDoctorRatingsAsync(doctorId)`, `GetClinicRatingsAsync(clinicId)`, `GetPlaceCleanlinessRatingsAsync(clinicId)` → `GET api/v1/doctors/{id}/ratings`, `GET api/v1/clinics/{id}/ratings`, `GET api/v1/clinics/{id}/place-cleanliness-ratings` — each returns `List<RatingDto>` | ClinicController, DoctorController |

### Response Models Pattern
- `ApiResponse<T>` — wrapper: `IsSuccess`, `Data`, `Errors`
- `PagginatedResult<T>` — paginated list: `Items`, `TotalCount`, `TotalPages`, `PageNumber`, `PageSize`, `HasPreviousPage`, `HasNextPage`
- DTOs per domain: `PlanDto`, `SpecializationDto`, `ClinicManagmentDto`, `ClinicDetailsDto` (extends ClinicManagmentDto with Doctors, Staff, Ratings), `DoctorBriefDto`, `StaffBriefDto`, `RatingDto` (`Id` Guid, `Type` int 1=Doctor/2=Clinic/3=PlaceCleanliness, `UserId` Guid, `UserName` string?, `DoctorId`/`ClinicId` Guid?, `Value` 1-5, `Review` string?, `CreatedAt`), `DoctorDto`, `StaffDto` (has `Image` property for profile picture), `StaffDashboardStatsDto`, `StaffAppointmentDto`, `StaffQueueItemDto`, `UserProfileDto` (`Id`, `FullName`, `Email`, `Gender` int 1-3 nullable, `PhoneNumber`, `BirthDate` nullable, `ProfilePictureUrl` relative `/files/...` — resolve via `IAttachmentUrlResolver.Resolve()`, `Language` string ("1"=en/"2"=ar), `Role` string — one of `User`/`Doctor`/`Staff`/`ClinicOwner`/`SuperAdmin`, `IsFreelanceDoctor` bool), `AuthResponseDto` (`AccessToken`, `RefreshToken`, `FullName`, `Email`, `Roles`, `Id`, `ClinicId` Guid?, `DoctorId` Guid? — doctor's **entity** Id (not user Id), `ProfilePictureUrl`, `IsFreelanceDoctor`), etc.
- Slots DTOs (patient booking): `AvailableSlotsDto` (`DoctorId`, `ClinicId`, `RequestedDate`, `Days`), `SlotDayDto` (`DayOfWeek` string — numeric tolerated, `WorkingHours`, `SlotDurationMinutes`, `Slots`), `WorkingHoursDto` (`From`, `To`), `SlotDto` (`Id`, `StartTime`, `EndTime`, `IsAvailable`). One `days` entry per availability row — same weekday may repeat with different durations.
- Payments DTOs: `AdminPaymentDto` (`Id` Guid, `Code` "#P-xxx", `Type` 0-2, `Payer`, `Amount` decimal, `Currency`, `Method` 0/1, `Status` 0-3, `Date`, `RefNumber`), `PaymentStatsDto` (`TodayRevenue`, `SubscriptionsRevenue`, `AppointmentsRevenue`, `AdsRevenue`, `PendingCount`, `SuccessCount`, `FailedCount`, `RefundedCount`), `PaymentDetailDto` (+ `PaymentTimelineEntryDto` `{Date, Text, Marker}` with marker ∈ info/success/danger), `EligibleClinicDto`, `AdPackageDto` (`Id`, `Name`, `NameAr`, `DescriptionAr`, `Price`, `DurationDays`, `IsActive`), `AdsOrderResponseDto` (has `PaymobRedirectUrl` + `TargetRedirectUrl` helper)

### Request Models
- `CreateDoctorRequest` now supports `Availabilities` (List of `DoctorAvailabilityItem`)
- `DoctorAvailabilityItem` DTO: `DayOfWeek` (int 0-6), `StartTime` (string HH:mm:ss), `EndTime` (string HH:mm:ss), `SlotDurationMinutes` (int, default 30)
- `BookAppointmentRequest` (patient booking): `ClinicId`, `DoctorId`, `Date` (YYYY-MM-DD), `StartTime`, `EndTime` — must be exactly the slot times returned by the slots endpoint; `PatientName`, `PatientPhone`
- `CreateUserRequest` used for admin user creation (accepts `availabilitiesJson` as additional form param)
- `CreateStaffRequest` has `FullName`, `Email`, `PhoneNumber`, `Password`, `ClinicId`, `Image` (string?)
- `UpdateStaffRequest` has `FullName`, `PhoneNumber`, `IsActive`, `Image` (string?)
- `UploadAttachmentRequest` — used for image uploads before creating/updating entities; accepts `IFormFile`, `int place` (1=User/Images, 5=Clinic/Images, 7=Doctor/Images, 13=Specialization/Icons), `MediaType` enum
- `UpdateProfileRequest` — my-profile update (`PATCH /auth/profile/update`): all fields optional — `FullName`, `PhoneNumber`, `BirthDate` (must not be in the future), `Gender` 1-3, `ProfileImageUrl` (path from avatar upload)
- Payments requests: `GetAdminPaymentsRequest` (`PageNumber`, `PageSize`, nullable `Type`/`Status`/`Method`, `FromDate`/`ToDate`, `SearchTerm`), `CreateManualPaymentRequest` (`PayerId` clinic Guid, `Type` 1/2 only, `Amount`, `Method` 0/1 only, optional `RefNumber` ≤50, `Notes` ≤500), `RefundPaymentRequest` (`Reason`), `CreateAdsOrderRequest` (`ClinicId`, `AdPackageId`, `DurationDays`, optional `ReturnUrl`)

### Controller DI Pattern
```csharp
public class SomeController : BaseController
{
    private readonly ISomeService _someService;

    public SomeController(ISomeService someService)
    {
        _someService = someService;
    }
}
```

## Error Handling — 3-Layer Pattern

```csharp
try
{
    var result = await _service.MethodAsync();
    ViewBag.Data = result;
}
catch (ApiException ex)
{
    ViewBag.ErrorMessage = ex.Message;
}
catch (Exception ex)
{
    ViewBag.ErrorMessage = "عذراً، حدث خطأ. يرجى المحاولة لاحقاً.";
    _logger.LogError(ex, "context");
}
```

### TempData for cross-request messages
- `TempData["SuccessMessage"]` — shown as success modal after redirect
- `TempData["ErrorMessage"]` / `TempData["Error"]` — shown as error modal after redirect
- Layouts auto-check these and call `showSuccessModal()` / `showErrorModal()`

## Design Tokens & Utilities

### Colors
`--clr-bg: #F2F5F9` | `--clr-primary: #2F9CCA` | `--clr-success: #10b981`
`--clr-warning: #f59e0b` | `--clr-info: #3b82f6` | `--clr-danger: #ef4444`
`--clr-text: #384152` | `--clr-text-secondary: #66748C` | `--clr-border: #D2D5DB`

### Spacing: `--space-1` (5px) through `--space-8` (40px)
### Typography: `--fs-h1` (40px), `--fs-h2` (26px), `--fs-lg` (16px), `--fs-md` (14px)
### Utility classes: `.text-h1`, `.text-h2`, `.text-lg`, `.text-lg-medium`, `.text-md`, `.text-md-medium`, `.text-md-bold`

### Component Classes
| Component | Classes |
|-----------|---------|
| Page header | `.page-header`, `.page-title`, `.page-title-icon`, `.subtitle-icon` |
| Stat cards | `.stats-grid`, `.stat-card`, `.stat-info`, `.stat-value`, `.stat-label` |
| Table card | `.table-card`, `.table-card-header`, `.table-card-title`, `.custom-table`, `.table-actions` |
| Filter bar | `.filter-bar` with `.form-input`/`.form-select` (`width: auto`) |
| Buttons | `.btn`, `.btn-primary`, `.btn-secondary`, `.btn-danger`, `.btn-sm`, `.btn-icon` |
| Badges | `.badge`, `.badge-success`, `.badge-warning`, `.badge-info`, `.badge-danger`, `.badge-primary` |
| Icon wrappers | `.icon-wrapper`, `.icon-wrapper--primary/--blue/--amber/--green` |
| Forms | `.form-group`, `.form-label`, `.form-input`, `.form-select`, `.form-textarea`, `.form-toggle` |
| Modals | `.modal-content-custom`, `.modal-header-custom`, `.modal-body-custom`, `.modal-footer-custom` |
| Pagination | `.pagination-nav`, `.pagination-list`, `.pagination-link`, `.pagination-link--active` |
| Status text | `.status-active`, `.status-inactive`, `.priority-high`, `.priority-medium`, `.priority-low` |
| Stepper / Wizard | `.stepper`, `.stepper-step`, `.stepper-step.active`, `.stepper-step.completed`, `.stepper-circle`, `.stepper-label`, `.stepper-line`, `.step-panel` |
| Availability rows | `.availability-row`, `.availability-day`, `.availability-from`, `.availability-to`, `.availability-slot`, `.availability-sep`, `.availability-remove-btn`, `.add-availability-btn` |
| Staff/Doctor cards grid | `.staff-grid` (grid container), `.staff-card` (card), `.staff-card-avatar`, `.staff-card-initial`, `.staff-card-body`, `.staff-card-name`, `.staff-card-role`, `.staff-card-contact`, `.staff-contact-link`, `.staff-card-exp`, `.staff-exp-badge` |
| Clinic detail header | `.clinic-detail-header`, `.cdh-image`, `.cdh-image-placeholder`, `.cdh-info`, `.cdh-name`, `.cdh-specs`, `.cdh-meta`, `.cdh-rating`, `.cdh-status`, `.cdh-desc`, `.cdh-contact`, `.cdh-actions` |
| Clinic stats row | `.clinic-stats-row`, `.clinic-stat-card`, `.clinic-stat-value`, `.clinic-stat-label` |
| Detail cards | `.detail-card`, `.detail-section-header`, `.detail-grid-2col`, `.detail-field`, `.detail-field-label`, `.detail-field-value` |
| Working hours table | `.wh-table` with table/th/td |
| Profile page | `.profile-page`, `.profile-card` (+`-header`/`-body`), `.profile-avatar-large`, `.profile-avatar-img`, `.profile-name-row`, `.profile-full-name`, `.profile-role-label`, `.profile-freelance-badge`, `.profile-edit-btn`, `.profile-field` (+`-icon`/`-label`/`-value`), `.profile-image-status`, `.header-avatar`, `.header-avatar-img` |
| Online booking slots | `.slot-segment` (+`-header`/`-title`/`-hours`), `.slot-duration-badge`, `.slot-grid`, `.slot-btn` (+`--selected`/`--disabled`), `.booking-filters`, `.booking-filter-group`, `.booking-empty`, `.booking-summary` (+`-item`) |
| Doctor detail header | `.doctor-detail-header`, `.dd-header-avatar`, `.dd-header-info`, `.dd-header-name`, `.dd-header-meta`, `.dd-header-spec`, `.dd-header-actions` |
| User detail layout (Admin Users pages) | `.user-detail-layout`, `.user-detail-content`, `.sub-sidebar`, `.sub-sidebar-header`, `.sub-sidebar-back`, `.sub-sidebar-item` |
| Public header mobile nav | `.public-nav-toggle` (hamburger), `.public-nav-collapse`, `.public-nav-links` |

### Responsive Conventions (established patterns — reuse, don't reinvent)

- **Breakpoints**: 1500 / 1300 / 1200 / 1100 / 992 / 991 / 768 / 576 / 480 / 420 px in `site.css`. All responsiveness lives in `site.css`; `design-system.css` has zero media queries (tokens + components only).
- **App sidebars** (Clinic/Admin/Doctor/Staff): `.sidebar` is a 264px fixed column ≥992px, becomes an off-canvas drawer (`right: -274px` → `right: 0` with `.mobile-open`) ≤991px, toggled by `.sidebar-toggle-mobile` (hamburger in `.top-header`).
- **Sub-sidebars** (`.sub-sidebar`, e.g. Admin Users pages): ≤991px the flex row stacks to column; the 200px sub-sidebar becomes a full-width horizontal scroll-tab bar (`.sub-sidebar-item { white-space: nowrap; flex-shrink: 0 }` inside `overflow-x: auto`).
- **Tables**: always wrap `<table class="custom-table">` in `<div class="table-responsive">` inside `.table-card`. ≤768px the `.table-card` scrolls horizontally (`min-width: max-content`), never the page (`html { overflow-x: clip }`). Optional progressive column hiding via `th:nth-child(n)`/`td:nth-child(n)` `display: none`.
- **Grids**: stat grids go 4→2→1 cols (1500/768), 3-col grids →1 at 992/768, `repeat(auto-fill, minmax(...))` grids must use `minmax(min(320px, 100%), 1fr)` so they never clip at 320px viewports.
- **Header action rows** (`.cdh-actions`, `.dd-header-actions`, `.page-header-actions`): `flex-wrap: wrap` at base; ≤576px column + full-width `justify-content: center` buttons.
- **Modals**: ≤576px become full-width bottom sheets (`.modal-content-custom` fixed to viewport edges).
- **Public site**: Bootstrap grid (`row g-4`, `col-md-*`) + clamp typography; mobile nav = Bootstrap collapse hamburger (`.public-nav-toggle`, `d-md-none`), data-attributes only — no custom JS. `.public-header .container` drops fixed 72px height ≤576px (`height: auto; min-height: 72px`).
- **Text safety**: `.auth-email` and other LTR data strings use `overflow-wrap: anywhere` + `max-width: 100%`. Section headers with a badge child (`.detail-section-header`) wrap (`flex-wrap: wrap; gap: var(--space-2)`).
- **Service worker**: `firebase-messaging-sw.js` embeds the full Firebase web config (worker context cannot read `window.ClinicHubConfig`); page config comes from `_FcmConfig.cshtml` ← `FirebaseWebOptions` ← `appsettings.*.json` `FirebaseWeb` section.

### Web Push / Notification Bell (all dashboards)

- **Enum**: `NotificationType` in `ClinicHub.Services/Enums/NotificationType.cs` — values 0–18 (`NewMessage`=1, `SubscriptionExpiring`=7, `AdExpiring`=9, `AppointmentOutsideAvailability`=10, `AppointmentOutsideWorkingHours`=11, `NewBookingRequest`=12, `ClinicRegistered`=13, `ClinicApproved`=14, `ClinicRejected`=15, `SupportTicketUpdate`=16, `PaymentReceived`=17, `RevenueIncreased`=18). Types 10–18 are dashboard-only; the rest are shared with the mobile catalogue. Docs: `docs/WEB_DASHBOARD_NOTIFICATIONS_README.md` + `docs/NOTIFICATIONS_README.md`.
- **Bell endpoints** (bearer token, any dashboard role): `GET api/v1/notifications/count` → `ApiResponse<int>` (unread count → badge); `GET api/v1/notifications/pagginated?pageNumber&pageSize` → `PagginatedResult<NotificationDto>`. The list endpoint marks returned items read server-side — refresh the badge after fetching the list.
- **NotificationDto fields**: display `titleAr`/`bodyAr` (Arabic) — `titleEn`/`bodyEn` are stored empty. `type` comes back as the numeric enum value (int).
- **fcm.js behaviour**: `TYPE_NAMES` maps numeric types → names; `navigateByType(type)` resolves per role — appointment types (incl. 10–12, 17–18) → role appointments page, clinic types (13–15) → `/Clinic/Index` (owner) or `/Admin/Clinics` (superadmin), `SupportTicketUpdate` → `/Clinic/Support` or `/Admin/Support`, remaining → notifications page. Role pages cached in Cache Storage (`__ch_nav_*` keys) for the service worker's `notificationclick` resolver.
- **Token registration**: `fcm.js` auto-attaches FCM tokens on the login form (`#loginForm`) and the clinic-registration form (`#clinicRegisterForm` — hidden `fcmToken` + `devicePlatform` inputs; token must be sent at registration time because a pending owner can't log in until approval). Token rotated/registered on dashboards via `POST /api/v1/auth/fcm-token`.

## Instructions

### Step 1: Read the plan & understand the endpoint
- If a plan exists, locate the current step
- Check which service interface matches the feature (from the Available Services table above)
- Review the request/response DTOs in `Services/RequestModels/` and `Services/ReponseModels/`

### Step 2: Scan existing files in the target area
- Read 2-3 existing views in the same controller area to match presentation style
- Read the controller to understand existing endpoint consumption patterns
- Check the route helper class for existing route patterns

### Step 3: Implement one deliverable at a time
- Do NOT write all files at once
- Order: controller action → route helper → view → CSS (if needed)
- Validate each step before moving to the next

### Step 4: Controller action pattern

#### Async endpoint (with API service)
```csharp
[Route("Admin/SomeAction/{id:guid}")]
public async Task<IActionResult> SomeAction(Guid id, int pageNumber = 1, int pageSize = 20)
{
    try
    {
        var result = await _service.MethodAsync(id, pageNumber, pageSize);
        ViewBag.Items = result.Items;
        ViewBag.Pagination = result;
        ViewBag.FilterParam = someValue;
    }
    catch (ApiException ex)
    {
        ViewBag.ErrorMessage = ex.Message;
        ViewBag.Items = new List<DtoType>();
    }
    return View();
}
```

#### 404 Fallback pattern (endpoint not yet implemented)
```csharp
ViewBag.Doctors = new List<DoctorBriefDto>();
ViewBag.Staff = new List<StaffBriefDto>();
ViewBag.RecentRatings = new List<RatingDto>();

try
{
    var details = await _service.GetClinicDetailsAsync(...);
    // populate ViewBag with DTO data
}
catch (ApiException ex) when (ex.StatusCode == 404)
{
    try
    {
        var fallback = await _service.GetBasicDataAsync(...);
        // populate ViewBag with basic data
    }
    catch (ApiException)
    {
        ViewBag.ErrorMessage = "...";
        ViewBag.Clinic = null;
    }
}
catch (ApiException ex)
{
    ViewBag.ErrorMessage = ex.Message;
}
```

#### JSON API endpoint (for AJAX)
```csharp
[HttpPost]
public async Task<IActionResult> SomeAction(Guid id)
{
    try
    {
        var result = await _service.MethodAsync(id);
        return Json(new { success = true, data = result });
    }
    catch (ApiException ex)
    {
        Response.StatusCode = ex.StatusCode;
        return Json(new { success = false, message = ex.Message });
    }
}
```

#### Doctor creation with availability (ClinicOwner — AJAX, 2-phase)
- Controller: `ClinicController.CreateDoctor` receives `JsonElement body`  
- **Phase 1**: Creates a new user via `_userService.CreateUserAsync(CreateUserRequest)` — accepts `fullName`, `email`, `phoneNumber`, `password`, `gender`, `birthDate`, `specializationId`, `bio`, `yearsOfExperience`
- **Phase 2**: Creates doctor profile with availability via `_clinicDoctorService.CreateDoctorAsync(CreateDoctorRequest)` using the `userId` from Phase 1  
- `availabilities` is a JSON array: `[{ dayOfWeek, startTime, endTime, slotDurationMinutes }]`
- Returns `{ success, message, data: DoctorDto }`

#### Admin CreateUser with availabilities
- `AdminController.CreateUser` accepts additional `[FromForm] string? availabilitiesJson`
- `availabilitiesJson` is a hidden input populated by JS before form submit
- Serializes working hours rows to JSON before POST

#### Staff create/edit with image upload (ClinicController — multipart form, 2-step)
- **Step 1**: Upload image via `_attachmentService.UploadAttachmentAsync(new UploadAttachmentRequest(file, 1, MediaType.Image))` where `1` = User/Images
- **Step 2**: Set `request.Image = fileName` from upload response, then call `_clinicStaffService.CreateStaffAsync(request)` or `_clinicStaffService.UpdateStaffAsync(id, request)`
- Controller reads form fields via `Request.Form["fieldName"]` and file via `Request.Form.Files.GetFile("imageFile")`
- View sends `FormData` with `imageFile` appended when user selects a file
- Image display URL: `{attachmentBaseUrl}/files/{filename}` (use `_doctoryOptions.Value.BaseUrl` in views, injected via `@inject IOptions<Doctory>`)

#### Profile feature (all dashboards — Admin/Clinic/Doctor/Staff)
- **GET** `Profile()` per controller → `ViewBag.Profile` (UserProfileDto) via `_authService.GetProfileAsync()`; 3-layer error handling
- **POST** `UpdateProfile()` — multipart form (not JSON): fields `fullName`, `phoneNumber`, `birthDate`, `gender`, optional `imageFile`; when a file is present, upload first via `_attachmentService.UploadAttachmentAsync(new UploadAttachmentRequest(file, 1, MediaType.Image))` (place `1` = User/Images), then build `UpdateProfileRequest` (only non-empty values) → `_authService.UpdateProfileAsync(request)`; returns `Json(new { success, message })` (on ApiException set `Response.StatusCode = ex.StatusCode`)
- **Layout header hydration**: every dashboard controller's `OnActionExecutionAsync` calls `await LoadHeaderProfileAsync(_authService)` (BaseController helper, silent-fail) → `ViewBag.HeaderProfile` (UserProfileDto) → layouts render real name/initial/photo with fallback placeholders. Layouts inject `IAttachmentUrlResolver` to build avatar URLs
- **Shared view**: `Views/Shared/_ProfilePage.cshtml` — reads `ViewBag.Profile` (falls back to `ViewBag.HeaderProfile`); role label from `profile.Role` string switch; freelance badge when `IsFreelanceDoctor`; edit modal (name/phone/birthdate/gender/avatar) posts `FormData` to `@Url.Action("UpdateProfile")` (resolves per-area); script must NOT use `@section` inside a partial — wrap in `document.addEventListener('DOMContentLoaded', ...)`
- **Area pages**: `Views/{Admin,Clinic,Doctor,Staff}/Profile.cshtml` — thin views: set `ViewData["Title"]` + `Layout` + `<partial name="_ProfilePage" />`
- **Routes**: `ClinicRoutes.Pages.Profile()`, `DoctorRoutes.Pages.Profile()`, `StaffRoutes.Pages.Profile()` (Admin already had `Profile()`); header/sidebar user blocks link to the area profile page

#### UserService.GetAllUsersPagginatedAsync query behavior
- When `UserTypes` list is **empty** (no filter): sends ALL non-None `UserType` enum values (User, SuperAdmin, Doctor, Staff, ClinicOwner) to the API
- When `UserTypes` list has values: sends only those specific `UserType` values
- Role filter values: `2`=SuperAdmin, `16`=ClinicOwner, `8`=Staff, `1`=User

### Step 5: View construction patterns

#### Layout selection
```cshtml
@{
    ViewData["Title"] = "عنوان الصفحة";
    Layout = "_AdminLayout"; // _ClinicLayout, _DoctorLayout, _StaffLayout, or none for public
}
```

#### Public header mobile nav pattern (no custom JS — Bootstrap collapse only)
```html
<div class="d-flex gap-2 align-items-center">
    <a href="@HomeRoutes.Account.Login()" class="btn-primary-custom">تسجيل الدخول</a>
    <button class="public-nav-toggle d-md-none" type="button" data-bs-toggle="collapse"
            data-bs-target="#publicNav" aria-expanded="false" aria-controls="publicNav" aria-label="فتح القائمة">
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
            <path d="M3 6h18M3 12h18M3 18h18"/>
        </svg>
    </button>
</div>
<!-- Collapse panel: sibling of .container inside .public-header -->
<div class="collapse d-md-none public-nav-collapse" id="publicNav">
    <div class="container public-nav-links">
        <a href="@HomeRoutes.Pages.Index()" class="nav-link">الرئيسية</a>
        <!-- ... links ... -->
    </div>
</div>
```

#### Page header pattern
```cshtml
<h2 class="page-title">
    <span class="page-title-icon">
        <svg viewBox="0 0 24 24" fill="currentColor"><path d="..."/></svg>
    </span>
    العنوان
</h2>
```

#### Stat card pattern (from API data)
```cshtml
<div class="stats-grid">
    <div class="stat-card">
        <div class="stat-info">
            <span class="stat-value">@stats.TotalCount</span>
            <span class="stat-label">الإجمالي</span>
        </div>
        <div class="icon-wrapper icon-wrapper--primary">
            <svg viewBox="0 0 24 24" fill="currentColor"><path d="..."/></svg>
        </div>
    </div>
</div>
```

#### Table pattern (from API paginated data)
```cshtml
<div class="table-card">
    <div class="table-card-header">
        <h3 class="table-card-title">
            <span class="subtitle-icon"><svg viewBox="0 0 24 24" fill="currentColor"><path d="..."/></svg></span>
            عنوان الجدول
        </h3>
    </div>
    <div class="table-responsive">
        <table class="custom-table">
            <thead>
                <tr>
                    <th>#</th>
                    <th>الاسم</th>
                    <th>الحالة</th>
                    <th>الإجراءات</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in ViewBag.Items)
                {
                    <tr>
                        <td class="text-md">@item.Code</td>
                        <td class="text-md-medium">@item.Name</td>
                        <td><span class="badge badge-@item.StatusClass">@item.StatusText</span></td>
                        <td class="table-actions">...</td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
</div>
```

#### Pagination rule (CRITICAL)
- If the endpoint returns paginated data (`PagginatedResult<T>`), the view **MUST** include `_Pagination.cshtml` partial after the table
- Controller passes `ViewBag.Pagination` with: `TotalCount`, `TotalPages`, `PageNumber`, `PageSize`, `HasPreviousPage`, `HasNextPage`
- Additional filters auto-read by partial: `CurrentFilter`, `SearchTerm`, `StatusFilter`, `DateFilter`, `IsUnassigned`
- Reference: `AdminController.Specializations()` at `Controllers/AdminController.cs:53`

#### Modal rule (CRITICAL)
- Include modals at view bottom before `@section Scripts`
- Use shared partials: `_SuccessModal`, `_ErrorModal`, `_ConfirmModal`
- Trigger via JS: `showSuccessModal(msg)`, `showErrorModal(msg)`, `showConfirmModal(msg, callback)`
- Detail views: use dedicated page with entity ID in route + back link to list
- Layouts already include modals globally, only add page-specific modals when needed

#### Filter bar pattern
```cshtml
<div class="filter-bar">
    <input class="form-input" type="text" placeholder="بحث..." value="@ViewBag.SearchTerm" />
    <select class="form-select" onchange="applyFilter()">
        <option value="">كل الحالات</option>
        <option value="active" selected="@(ViewBag.StatusFilter == "active")">نشط</option>
        <option value="inactive" selected="@(ViewBag.StatusFilter == "inactive")">غير نشط</option>
    </select>
</div>
```

#### Table action buttons pattern
```cshtml
<td class="table-actions">
    <button class="btn-icon" onclick="showDetail(@item.Id)" title="عرض">
        <svg viewBox="0 0 24 24" fill="currentColor"><path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z"/></svg>
    </button>
    <button class="btn-icon" onclick="editItem(@item.Id)" title="تعديل">
        <svg viewBox="0 0 24 24" fill="currentColor"><path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/></svg>
    </button>
    <button class="btn-icon" onclick="deleteItem(@item.Id)" title="حذف">
        <svg viewBox="0 0 24 24" fill="currentColor"><path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z"/></svg>
    </button>
</td>
```

#### Attachment base URL injection (for image display in views)
```cshtml
@using ClinicHub.Services.Options
@inject Microsoft.Extensions.Options.IOptions<Doctory> _doctoryOptions
@{
    var attachmentBaseUrl = _doctoryOptions.Value.BaseUrl.TrimEnd('/');
}
```
Use `attachmentBaseUrl` to construct image URLs: `@(attachmentBaseUrl)/files/@filename`
Pass to JS via: `var attachmentBaseUrl = '@attachmentBaseUrl';`

#### View cast pattern (fallback-safe for inherited DTOs)
```cshtml
@{
    var clinicDetails = ViewBag.Clinic as ClinicDetailsDto;
    var clinic = clinicDetails ?? (ViewBag.Clinic as ClinicManagmentDto);
    var doctors = clinicDetails?.Doctors ?? new List<DoctorBriefDto>();
    var staffList = clinicDetails?.Staff ?? new List<StaffBriefDto>();
    var recentRatings = clinicDetails?.RecentRatings ?? new List<RatingDto>();
    var avgRating = clinicDetails?.AverageRating ?? clinic.Rating ?? 0;
    var totalRatings = clinicDetails?.TotalRatings ?? 0;
}
```

#### Tab-based detail page pattern (clinic details)
```cshtml
<div class="detail-tabs mt-3">
    <ul class="nav nav-tabs" id="clinicTabs" role="tablist">
        <li class="nav-item"><button class="nav-link active" id="ov-tab" data-bs-toggle="tab" data-bs-target="#ov" type="button" role="tab">نظرة عامة</button></li>
        <li class="nav-item"><button class="nav-link" id="docs-tab" data-bs-toggle="tab" data-bs-target="#docs" type="button" role="tab">الأطباء</button></li>
        <li class="nav-item"><button class="nav-link" id="stf-tab" data-bs-toggle="tab" data-bs-target="#stf" type="button" role="tab">الموظفين</button></li>
        <li class="nav-item"><button class="nav-link" id="ratings-tab" data-bs-toggle="tab" data-bs-target="#ratings" type="button" role="tab">التقييمات</button></li>
    </ul>
    <div class="tab-content">
        <div class="tab-pane fade show active" id="ov" role="tabpanel">...</div>
        <div class="tab-pane fade" id="docs" role="tabpanel">...</div>
        <div class="tab-pane fade" id="stf" role="tabpanel">...</div>
        <div class="tab-pane fade" id="ratings" role="tabpanel">...</div>
    </div>
</div>
```

#### Staff/Doctor card grid pattern (replace tables for profile lists)
```html
<div class="staff-grid">
    <div class="staff-card">
        <div class="staff-card-avatar">
            <img src="..." alt="..." />
            <!-- or: <span class="staff-card-initial">أ</span> -->
        </div>
        <div class="staff-card-body">
            <h4 class="staff-card-name">الاسم</h4>
            <span class="staff-card-role">الدور/التخصص</span>
            <div class="staff-card-contact">
                <a href="mailto:..." class="staff-contact-link">البريد</a>
                <a href="tel:..." class="staff-contact-link">الهاتف</a>
            </div>
        </div>
        <div class="staff-card-exp">
            <span class="staff-exp-badge">5+ سنوات</span>
        </div>
    </div>
</div>
```

#### Ratings list pattern
```html
<div class="ratings-header-card">
    <div class="rhc-score">
        <span class="rhc-number">4.5</span>
        <div class="stars-inline"><!-- SVG stars --></div>
        <span class="rhc-count">12 تقييم</span>
    </div>
</div>
<div class="ratings-list">
    <div class="rating-full-card">
        <div class="rfc-avatar">م</div>
        <div class="rfc-content">
            <div class="rfc-header">
                <span class="rfc-name">المستخدم</span>
                <span class="rfc-date">2026-07-20</span>
            </div>
            <div><!-- stars --></div>
            <p class="rfc-comment">نص المراجعة</p>
        </div>
    </div>
</div>
```

#### Ratings pages (Clinic/Ratings + Doctor/Ratings)
- **Endpoints** (read-only display; submission is mobile-side via `POST api/v1/ratings`): `GET /doctors/{doctorId}/ratings` (type=Doctor), `GET /clinics/{clinicId}/ratings` (type=Clinic), `GET /clinics/{clinicId}/place-cleanliness-ratings` (type=PlaceCleanliness). All `[RoleAuthorize]` — any authenticated role; web dashboard only reads.
- **Rating types** (backend `RatingType` int enum): `1` Doctor, `2` Clinic, `3` PlaceCleanliness. Each section submits its own row — a user may rate the clinic AND cleanliness separately.
- **Controller** — `ClinicController.Ratings()` → ViewBag: `Ratings` (clinic ratings), `CleanlinessRatings`, `AverageRating`, `TotalRatings`, `CleanlinessAverage`, `TotalCleanlinessRatings`, `ErrorMessage`; uses `CurrentUser.ClinicId`. `DoctorController.Ratings()` → ViewBag: `Ratings`, `AverageRating`, `TotalRatings`, `ErrorMessage`; uses `CurrentUser.DoctorId` (mock fallback: `MockIds.Doctor(CurrentUser.Id)`).
- **Doctor identity** — ratings are keyed by the Doctor **entity** Id, NOT the user Id. The backend login response exposes it as `AuthResponseDto.DoctorId`; frontend flows: `AccountController.SetAuthCookies` → `TempData["DoctorId"]` → layouts `localStorage.setItem("doctorId", ...)` → `CurrentUserContext.DoctorId`. Mock contexts set `DoctorId = MockIds.Doctor(n)`.
- **Views** — `Views/Clinic/Ratings.cshtml` (two `detail-card` sections: تقييم العيادة + نظافة المكان, each with `ratings-header-card` avg + `ratings-list` cards) and `Views/Doctor/Ratings.cshtml` (single summary card + list). Reuse existing CSS: `.ratings-header-card`, `.rhc-score`/`-number`/`-count`, `.stars-inline`, `.ratings-list`, `.rating-full-card`, `.rfc-avatar`/`-content`/`-header`/`-name`/`-date`/`-comment` (site.css "Ratings Tab" section).
- **Routes** — `ClinicRoutes.Pages.Ratings()` → `/Clinic/Ratings`, `DoctorRoutes.Pages.Ratings()` → `/Doctor/Ratings`; sidebar links in `_ClinicLayout` (after Support) and `_DoctorLayout` (under إدارة المواعيد), icon `bi bi-star`.
- **API routes** — `DoctoryRoutes.Ratings`: `Create`, `DoctorRatings(doctorId)`, `ClinicRatings(clinicId)`, `PlaceCleanlinessRatings(clinicId)`.

### Step 5a: Multi-step wizard (stepper + step panels)
Use for multi-stage forms. Applies to "Add Doctor" flow (3 steps: personal info → professional info → working hours).

```html
<!-- Stepper indicator -->
<div class="stepper">
    <div class="stepper-step active" data-step="1">
        <div class="stepper-circle">1</div>
        <div class="stepper-label">المعلومات الشخصية</div>
    </div>
    <div class="stepper-line"></div>
    <div class="stepper-step" data-step="2">
        <div class="stepper-circle">2</div>
        <div class="stepper-label">المعلومات المهنية</div>
    </div>
    <div class="stepper-line"></div>
    <div class="stepper-step" data-step="3">
        <div class="stepper-circle">3</div>
        <div class="stepper-label">أوقات العمل</div>
    </div>
</div>

<!-- Step panels -->
<div class="step-panel" id="step1Panel">...</div>
<div class="step-panel" id="step2Panel" style="display:none;">...</div>
<div class="step-panel" id="step3Panel" style="display:none;">...</div>

<!-- Navigation -->
<button class="btn" id="stepPrevBtn">السابق</button>
<button class="btn" id="stepNextBtn">التالي</button>
<button class="btn" id="stepSubmitBtn" style="display:none;">إضافة الطبيب</button>
```

Stepper JS pattern:
```javascript
var currentStep = 1, totalSteps = 3;

function updateStepper(step) {
    $('.stepper-step').each(function () {
        var s = parseInt($(this).data('step'));
        $(this).removeClass('active completed');
        if (s === step) $(this).addClass('active');
        else if (s < step) $(this).addClass('completed');
    });
    $('.step-panel').hide();
    $('#step' + step + 'Panel').show();
    $('#stepPrevBtn').toggle(step > 1);
    $('#stepNextBtn').toggle(step < totalSteps);
    $('#stepSubmitBtn').toggle(step === totalSteps);
}

function validateStep(step) {
    // validate fields in current step, showErrorModal if invalid
    return true;
}

$('#stepNextBtn').on('click', function () {
    if (!validateStep(currentStep)) return;
    goToStep(currentStep + 1);
});
```
### Step 5b: Availability rows pattern (dynamic add/remove)
```html
<div id="availabilityContainer">
    <div class="availability-row">
        <select class="availability-day form-select auth-form-control">
            <option value="">اليوم</option>
            <option value="0">الأحد</option> ... <option value="6">السبت</option>
        </select>
        <input type="time" class="availability-from form-control auth-form-control" value="09:00" />
        <span class="availability-sep">إلى</span>
        <input type="time" class="availability-to form-control auth-form-control" value="17:00" />
        <input type="number" class="availability-slot form-control auth-form-control" value="30" min="1" max="480" placeholder="دقيقة" />
        <button type="button" class="availability-remove-btn" title="حذف"><i class="bi bi-x-circle"></i></button>
    </div>
</div>
<button type="button" id="addAvailabilityBtn" class="btn btn-sm add-availability-btn">
    <i class="bi bi-plus-circle"></i> إضافة يوم
</button>
```

JS availability management pattern:
```javascript
var DAY_NAMES = ['الأحد', 'الإثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];
function addAvailabilityRow(day, fromTime, toTime, slotMin) {
    // builds row HTML with DAY_NAMES.map
    // attaches .availability-remove-btn click handler
}
// Submit: serialize .availability-row data into availabilities array
$('.availability-row').each(function() {
    availabilities.push({
        dayOfWeek: parseInt(row.find('.availability-day').val()),
        startTime: row.find('.availability-from').val() + ':00',
        endTime: row.find('.availability-to').val() + ':00',
        slotDurationMinutes: parseInt(row.find('.availability-slot').val()) || 30
    });
});
```

### Step 5c: Fixed slot duration (مدة الموعد ثابتة 30 دقيقة) — one rule everywhere

> **Rule:** Appointment duration is **fixed at 30 minutes** for all doctors/days/clinics. Doctors cannot change it. Authoritative spec: `docs/FIXED_SLOT_DURATION.md`.

- **Patient booking page** (`Clinic/OnlineBooking`): consumes the **slots** endpoint only — `GET /api/v1/clinics/{clinicId}/doctors/{doctorId}/slots?date=YYYY-MM-DD` via `IClinicDoctorService.GetAvailableSlotsAsync(clinicId, doctorId, date)`. Response `data.days[]` has **one entry per availability row** (`dayOfWeek`, `workingHours {from,to}`, `slotDurationMinutes`, `slots[{id,startTime,endTime,isAvailable}]`). Each segment is labeled with the fixed duration "30 دقيقة لكل موعد" — never render the backend value. `isAvailable:false` = overlaps a booked appointment; render disabled.
  - **Booking-window clamp**: date input `min=today`, `max=today+maxAdvanceBookingDays` (default 30, from clinic settings endpoint → `ClinicSettingsDto.MaxAdvanceBookingDays`). Dates beyond the window → HTTP 400 localized `Booking.InvalidDate`; the page shows the localized message (also JS-guards client-side).
  - **Submit**: `BookSlot` controller action posts `BookAppointmentRequest` with the **exact** `startTime`+`endTime` from the returned slot — made-up times → HTTP 400 localized `AppointmentMessages.DoctorNotAvailableAtThisTime`. Errors surface via `showErrorModal(res.message)` (localized, Arabic via `Accept-Language: ar` on the HttpClient).
  - Controller actions: `ClinicController.OnlineBooking()` (ViewBag: `ClinicId`, `Doctors` list, `MaxAdvanceBookingDays`; silent fallback for settings), `GetDoctorSlots(doctorId, date)` and `BookSlot([FromBody] JsonElement)` JSON endpoints with 3-layer error handling.
- **Doctor dashboard** (`Doctor/Availability`): `GET/PUT /api/v1/doctors/availability(/week)` via `IDoctorService` — flat rows with per-row `slotDurationMinutes` (always 30). **No duration input in rows** (`.availability-slot` removed); badge/stat show "مدة الحجز: 30 دقيقة (ثابتة)". Empty schedule → seed Sun–Thu 09:00–17:00 rows, 30 min, **no ids**. Save: rows from server keep `data-id`, new rows omit id; payload always sends `slotDurationMinutes: 30`; re-render from response (fresh ids). Client validates: endTime after startTime.
- **Clinic owner add/edit doctor** (`Clinic/Doctors`): create + edit availability rows have **no duration input**; payload always sends `slotDurationMinutes: 30`; detail view shows "30 دقيقة ثابتة". Controller (`ClinicController.CreateDoctor`/`UpdateDoctor`) forces `SlotDurationMinutes = 30`.
- **Admin user creation** (`Admin/Users/Index`): availability rows have **no duration input**; serialized payload always sends `slotDurationMinutes: 30`.
- **Clinic settings** (`Clinic/Settings`): shows fixed rule "مدة الموعد ثابتة: 30 دقيقة — تُطبق على جميع الأطباء والأيام ولا يمكن تغييرها" — read-only, not derived from doctor working hours (`ViewBag.TypicalSlotDuration` = 30 from `ClinicController.Settings`).
- CSS for the booking page: `.slot-segment`, `.slot-segment-header`, `.slot-segment-title`, `.slot-segment-hours`, `.slot-duration-badge`, `.slot-grid`, `.slot-btn`, `.slot-btn--selected`, `.slot-btn--disabled`, `.booking-filters`, `.booking-filter-group`, `.booking-empty`, `.booking-summary`, `.booking-summary-item`.

### Step 5d: Admin payments page (الدفعات والمعاملات المالية)

Consumes `IAdminPaymentService` (8 endpoints: list, detail, stats, manual, refund, eligible-clinics, packages, orders). Full spec: `docs/superadmin-payments-frontend-guide.md`.

- **Controller** — `AdminController.Payments(pageNumber, pageSize, type, status, method, fromDate, toDate, searchTerm, month)` sets ViewBag: `Payments` (`IReadOnlyCollection<AdminPaymentDto>`), `Pagination`, `Stats` (`PaymentStatsDto`), `Clinics` (`List<ClinicLookupDto>` from `IClinicService.GetAllClinicsForViewingOnlyAsync` — payer dropdown for type=1), `EligibleClinics` (`List<EligibleClinicDto>` — payer dropdown for type=2 + ads modal), `AdPackages` (`List<AdPackageDto>`), filter mirrors: `TypeFilter`/`StatusFilter`/`MethodFilter`/`FromDateFilter`/`ToDateFilter`/`SearchTerm`, plus `Month`/`MonthFilter` (`yyyy-MM`, defaults to current month). Each API call in its own try/catch (`ApiException`) with empty-list fallbacks.
- **Monthly stats** — stats are always month-scoped: `Payments` action parses `month` → `statsFromDate` (1st) + `statsToDate` (last day) → `GetPaymentStatsAsync(fromDate, toDate)` sends `FromDate`/`ToDate` query params. Month selector `<input type="month" id="statsMonthInput">` (auto-submits on change) + hidden `month` input in the filter bar + `month` route value in `_Pagination.cshtml` (ViewBag `MonthFilter`). First card labeled إيرادات الشهر. Backend contract: `docs/superadmin-monthly-stats-api.md` (requires backend support — dates param on `GET /admin/payments/stats`).
- **Detail** — `PaymentsDetails(Guid id)` route `Admin/PaymentsDetails/{id:guid}` → `ViewBag.Detail` (`PaymentDetailDto`); 404 → `ViewBag.ErrorMessage = "المعاملة غير موجودة"`.
- **POST JSON endpoints** (3-layer error handling, `Response.StatusCode = ex.StatusCode` on failure):
  - `CreateManualPayment([FromBody] CreateManualPaymentRequest)` → success `{ success, message, data }` (201)
  - `RefundPayment(Guid id, [FromBody] RefundPaymentRequest)` → `{ success, message }`
  - `CreateAdsOrder([FromBody] CreateAdsOrderRequest)` → `{ success, data: AdsOrderResponseDto }`; sets `ReturnUrl = {scheme}://{host}/Home/PaymentResult` when missing
- **Enums (backend)** — `Type`: 0 موعد مريض / 1 اشتراك عيادة / 2 خدمة إعلانية. `Status`: 0 معلق / 1 ناجح / 2 فاشل / 3 مسترد. `Method`: **only** 0 نقدي / 1 Paymob محفظة (no bank transfer / card).
- **Manual payment modal rules** — subscription-only (type fixed to `1` — the type select and ads payer dropdown were removed; ads flow lives exclusively in the ads modal): payer dropdown = `Clinics` (all clinics), method only نقدي / Paymob محفظة; on success `showSuccessModal` + `location.reload()`.
- **Ads flow** — ads modal: eligible clinic + package (`AdPackageDto`, prefill duration from `data-duration`) + duration days → `CreateAdsOrder` → open `data.paymobRedirectUrl` in new tab; `403` message `لا يمكن شراء الخدمات الإعلانية إلا للباقات المتقدمة` shown via `showErrorModal`.
- **Refund** — details page button only when `Status == 1` (ناجح); confirm modal with reason textarea → `RefundPayment`; on success reload.
- **Pagination** — `_Pagination.cshtml` preserves payment filters via ViewBag keys `TypeFilter`/`MethodFilter`/`FromDateFilter`/`ToDateFilter`/`MonthFilter` (mapped to route values `type`/`method`/`fromDate`/`toDate`/`month`; null values omitted for other pages).
- **CSS** — `.page-header-actions` (flex row for header buttons) in design-system.css; KPI cards reuse `.stats-grid`/`.stat-card`/`.icon-wrapper--{primary,blue,green,amber}`.

### Step 5e: Create subscription (superadmin grants a clinic a subscription)

- **Page** — `Admin/SubscriptionManagement` (إدارة الاشتراكات): "إضافة اشتراك" header button opens `createSubscriptionModal` (clinic select from `ViewBag.Clinics` via `IClinicService.GetAllClinicsForViewingOnlyAsync`, plan select from `ViewBag.Plans` filtered `IsActive` with `data-monthly`/`data-yearly` attributes, period select 0/1, start-date defaulting to today, read-only price preview updated by `updateSubPrice()`).
- **Controller** — `AdminController.SubscriptionManagement` also loads `ViewBag.Clinics`; `CreateSubscription([FromBody] CreateSubscriptionRequest)` POST JSON at `/Admin/SubscriptionManagement/Create` (3-layer error handling) → `{ success, message, data: SubscriptionDto }`.
- **Contract** — `CreateSubscriptionRequest` (`ClinicId`, `PlanId`, `Period` 0/1, optional `StartDate`, optional `Amount` — frontend does NOT send amount). Frontend sends only clinicId/planId/period/startDate; backend defaults amount to plan price.
- **Business rule** — the backend must create an **active** subscription so `GET /subscriptions/my` returns it immediately (clinic owner dashboard unlocks — `HasActivePlan` computed on every ClinicController request); if the clinic already has an active subscription the backend should extend it or reject with a localized message.
- **Route helper** — `DoctoryRoutes.AdminSubscriptions.CreateSubscription` (same URL as `ListSubscriptions`, different verb).

### Step 5f: Ads feature (الإعلانات — clinic owner + superadmin + mobile display)

Full spec: `docs/superadmin-ads-feature.md`. Backend contract (all endpoints below MISSING on backend): `docs/ads-backend-contract.md`. Statuses: 0 معلق الدفع / 1 نشط / 2 منتهي / 3 ملغي. Pricing: `amount = package.Price × (durationDays / package.DurationDays)`; instant activation after payment (Paymob webhook OR admin manual payment type 2 activates the clinic's latest pending ad); ads keep running until EndDate even if subscription lapses; 403 gate `لا يمكن شراء الخدمات الإعلانية إلا للباقات المتقدمة` enforced backend-side.

- **Clinic page** — `ClinicController.Marketing()` (async) → ViewBag: `Ads` (`List<AdDto>`), `Packages` (`List<AdPackageDto>` active); eligibility computed in view from `ViewBag.CurrentUser` (`CurrentUserContext`): `HasFeature(PlanFeature.MarketingTools) && HasActivePlan` → upsell card (ترقية الباقة → `Clinic/MySubscription`) when not eligible. Buy modal: package select (`data-duration`/`data-price`) + duration + read-only price preview (`updateBuyPrice()`) → POST `/Clinic/CreateAdOrder` JSON → `{ success, data: AdsOrderResponseDto }` → `window.open(data.paymobRedirectUrl)` + reload. `CreateAdOrder([FromBody] CreateAdsOrderRequest)` sets `ReturnUrl` default like the admin action; `clinicId` from `CurrentUser.ClinicId` (NOT from request).
- **Admin page** — `AdminController.Ads(pageNumber, pageSize, status)` route `Admin/Ads` → ViewBag: `Ads`, `Pagination`, `Packages` (all, incl. inactive), `StatusFilter`; `_Pagination.cshtml` preserves `status` via `ViewBag.StatusFilter`. Two tabs (Bootstrap nav-tabs): الإعلانات table (status badges 0-3, actions: `تسجيل دفعة نقدية` only for status 0 → reuses `/Admin/CreateManualPayment` `{ payerId: ad.ClinicId, type: 2, amount, method: 0, notes }`; `إلغاء` for status 0/1 → `/Admin/DeactivateAd?id=`), الباقات table + `#packageModal` CRUD → `/Admin/CreateAdPackage`, `/Admin/UpdateAdPackage?id=`, `/Admin/DeleteAdPackage?id=` (all POST JSON, 3-layer error handling).
- **Sidebar/links** — `_AdminLayout` إدارة الإعلانات under المالية (`AdminRoutes.Pages.Ads()`); `_ClinicLayout` link relabeled الخدمات الإعلانية (still gated by `canShowMarketing`).
- **DTOs** — `AdDto` (Id, ClinicId, ClinicName, PackageId, PackageNameAr, DurationDays, Amount, Currency, Status, StartDate?, EndDate?, CreatedAt), `UpsertAdPackageRequest` (Name, NameAr, Description, DescriptionAr, Price, DurationDays, IsActive). Routes: `DoctoryRoutes.AdminAds` (List, EligibleClinics, Packages, Package(id), Orders, Deactivate(id)) + new `DoctoryRoutes.Ads` (MyAds(clinicId), CreateOrder(clinicId), Packages, PublicActive).
- **CSS** — `.upsell-card`/`.upsell-icon`/`.upsell-body`/`.upsell-title`/`.upsell-text`, `.packages-row`/`.package-mini-card`(-name/-desc/-price), `.form-hint` in site.css.

### Step 6: Follow conventions strictly
- Use design token variables, never hardcoded colors/spacing
- Use existing utility classes before writing new CSS, add new CSS only in `site.css`
- NO inline `style=""` attributes
- Arabic labels, RTL (`dir="rtl"`, `lang="ar"`)
- Route helper for URLs (`@SomeRoutes.Pages.Action()`), never hardcoded strings
- Controller passes data via `ViewBag` only — NO `@model` directives
- Wrap all async API calls in `try/catch(ApiException)` + `catch(Exception)`
- Use `PagginatedResult<T>` for paginated endpoints

### Step 7: Validate
- Build: `dotnet build` in `ClinicHub/` directory
- Check for syntax errors in CSHTML and CSS
- Ensure no existing functionality is broken
- Summarize what was implemented and what remains

### Step 8: On failure
- Report error with exact file/line
- Suggest fix — do not silently retry
- If fix is straightforward, apply immediately

## Activation examples
- "نفّذ الخطوة التالية من الخطة"
- "اكتب كود صفحة إدارة التخصصات"
- "طبّق واجهة المستخدم لتسجيل العيادات"
- "Implement the staff dashboard queue view"
- "Create the endpoint integration for clinic settings"

## Non-activation examples
- "خطط لي إزاي أعمل الميزة دي" → planning
- "راجع الكود ده" → code review
