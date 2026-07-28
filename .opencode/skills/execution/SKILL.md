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
│   ├── BaseController — abstract base: CurrentUser, IsAjaxRequest, RedirectJson(), Fail()
│   ├── HomeController — public pages (subscriptions, clinic registration, attachments)
│   ├── AccountController — auth (login, forgot/reset password, verification)
│   ├── AdminController — system admin (clinics, doctors, specializations, payments, users, support)
│   ├── ClinicController — clinic owner/manager (appointments, doctors, staff, billing, settings)
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
├── Views/Shared/ — Layouts + partials (_Pagination, _SuccessModal, _ErrorModal, _ConfirmModal)
└── wwwroot/css/
    ├── design-system.css — Design tokens + utility classes
    └── site.css — Layout styles (sidebar, tables, cards, grid)
```

## Integration Layer (Service Contracts)

All controllers consume backend API through typed service interfaces injected via DI:

### Available Services
| Interface | Purpose | Used By |
|-----------|---------|---------|
| `IAuthService` | Login, refresh, logout | AccountController |
| `IPlanService` | Subscription plans CRUD | HomeController, AdminController, ClinicController |
| `ISubscriptionService` | Clinic subscriptions, registration | HomeController, ClinicController |
| `IAdminSubscriptionService` | Admin subscription management | AdminController |
| `ISpecializationService` | Medical specializations CRUD | HomeController, AdminController, ClinicController |
| `IClinicService` | Clinic management (CRUD, activate/deactivate) | AdminController |
| `IClinicDoctorService` | Clinic-specific doctor management (create with availability) | ClinicController |
| `IClinicStaffService` | Clinic-specific staff management | ClinicController |
| `IDoctorService` | Doctor management (admin) | AdminController |
| `IUserService` | User management, search (SearchUsers with IsUnassigned flag) | AdminController, ClinicController |
| `IUserVerificationService` | Verification requests management | AdminController |
| `IStaffDashboardService` | Staff dashboard (stats, queue, appointments) | StaffController |
| `IAttachmentService` | File uploads | HomeController, AdminController |
| `IAttachmentUrlResolver` | Resolve attachment URLs | AdminController |

### Response Models Pattern
- `ApiResponse<T>` — wrapper: `IsSuccess`, `Data`, `Errors`
- `PagginatedResult<T>` — paginated list: `Items`, `TotalCount`, `TotalPages`, `PageNumber`, `PageSize`, `HasPreviousPage`, `HasNextPage`
- DTOs per domain: `PlanDto`, `SpecializationDto`, `ClinicManagmentDto`, `DoctorDto`, `StaffDto`, `StaffDashboardStatsDto`, `StaffAppointmentDto`, `StaffQueueItemDto`, etc.

### Request Models
- `CreateDoctorRequest` now supports `Availabilities` (List of `DoctorAvailabilityItem`)
- `DoctorAvailabilityItem` DTO: `DayOfWeek` (int 0-6), `StartTime` (string HH:mm:ss), `EndTime` (string HH:mm:ss), `SlotDurationMinutes` (int, default 30)
- `CreateUserRequest` used for admin user creation (accepts `availabilitiesJson` as additional form param)

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

### Step 5: View construction patterns

#### Layout selection
```cshtml
@{
    ViewData["Title"] = "عنوان الصفحة";
    Layout = "_AdminLayout"; // _ClinicLayout, _DoctorLayout, _StaffLayout, or none for public
}
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
