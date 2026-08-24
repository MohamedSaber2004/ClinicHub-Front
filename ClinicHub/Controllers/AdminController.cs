using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;
using ClinicHub.Routes;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.Enums;
using ClinicHub.Services.RequestModels;
using Microsoft.Extensions.Options;

namespace ClinicHub.Controllers
{
    public class AdminController : BaseController
    {
        private readonly ISpecializationService _specializationService;
        private readonly IAttachmentUrlResolver _attachmentUrlResolver;
        private readonly IUserVerificationService _userVerificationService;
        private readonly IUserService _userService;
        private readonly IDoctorService _doctorService;
        private readonly IClinicService _clinicService;
        private readonly IAttachmentService _attachmentService;
        private readonly IOptions<GoogleMapsOptions> _googleMapsOptions;
        private readonly IPlanService _planService;
        private readonly IAdminSubscriptionService _adminSubscriptionService;
        private readonly IAdminDashboardService _adminDashboardService;
        private readonly IAuthService _authService;
        private readonly IAdminPaymentService _adminPaymentService;
        private readonly IAdService _adService;
        private readonly INotificationService _notificationService;
        private readonly IPlatformSettingService _platformSettingService;
        private readonly IClinicDoctorService _clinicDoctorService;

        public AdminController(ISpecializationService specializationService, IAttachmentUrlResolver attachmentUrlResolver, IUserVerificationService userVerificationService, IUserService userService, IDoctorService doctorService, IClinicService clinicService, IAttachmentService attachmentService, IOptions<GoogleMapsOptions> googleMapsOptions, IPlanService planService, IAdminSubscriptionService adminSubscriptionService, IAdminDashboardService adminDashboardService, IAuthService authService, IAdminPaymentService adminPaymentService, IAdService adService, INotificationService notificationService, IPlatformSettingService platformSettingService, IClinicDoctorService clinicDoctorService)
        {
            _specializationService = specializationService;
            _attachmentUrlResolver = attachmentUrlResolver;
            _userVerificationService = userVerificationService;
            _userService = userService;
            _doctorService = doctorService;
            _clinicService = clinicService;
            _attachmentService = attachmentService;
            _googleMapsOptions = googleMapsOptions;
            _planService = planService;
            _adminSubscriptionService = adminSubscriptionService;
            _adminDashboardService = adminDashboardService;
            _authService = authService;
            _adminPaymentService = adminPaymentService;
            _adService = adService;
            _notificationService = notificationService;
            _platformSettingService = platformSettingService;
            _clinicDoctorService = clinicDoctorService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // ── Layer 1: identity straight from the signed JWT ──────────────────────
            // The UserTypes claim is a bitmask written by the backend at login
            // (None=0, User=1, SuperAdmin=2, Doctor=4, Staff=8, ClinicOwner=16).
            // Reading it directly removes any dependency on profile-DTO mapping drift.
            bool tokenIsSuperAdmin = TokenGrantsSuperAdmin(context.HttpContext);

            // ── Layer 2: live profile check (also refreshes the header identity) ────
            UserProfileDto? profile = null;
            bool profileLoaded = false;
            try
            {
                profile = await _authService.GetProfileAsync();
                profileLoaded = true;
            }
            catch (ApiException ex) when (ex.StatusCode == 401 || ex.StatusCode == 403)
            {
                var loginUrl = $"{HomeRoutes.Account.Login()}?returnUrl={Uri.EscapeDataString(context.HttpContext.Request.Path + context.HttpContext.Request.QueryString)}";
                context.Result = IsAjaxRequest
                    ? new JsonResult(new { redirectUrl = loginUrl })
                    : new RedirectResult(loginUrl);
                return;
            }
            catch
            {
                // API unreachable — a token-holder may still proceed (read-only identity),
                // everyone else is denied because we cannot verify them.
                if (!tokenIsSuperAdmin)
                {
                    Response.StatusCode = 503;
                    context.Result = new ViewResult { ViewName = "ServiceUnavailable" };
                    return;
                }
            }

            bool isSuperAdmin = tokenIsSuperAdmin ||
                                (profileLoaded && profile != null &&
                                 string.Equals(profile.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase));

            if (!isSuperAdmin)
            {
                // A valid-but-different identity almost always means another account was
                // signed in from the same browser afterwards — tabs share ONE cookie jar,
                // so the newer login silently replaced this session. Say so plainly and
                // route to login with returnUrl for a one-click recovery.
                TempData["ErrorMessage"] = profileLoaded && profile != null
                    ? $"تم تبديل جلستك في هذا التبويب بسبب تسجيل الدخول بحساب آخر ({profile.FullName}) من نفس المتصفح. هذه الصفحة مخصصة لحسابات المشرف العام فقط — سجّل الدخول مجدداً للمتابعة."
                    : "هذه الصفحة مخصصة لحسابات المشرف العام فقط.";

                var adminLoginUrl = $"{HomeRoutes.Account.Login()}?returnUrl={Uri.EscapeDataString(context.HttpContext.Request.Path + context.HttpContext.Request.QueryString)}";
                context.Result = IsAjaxRequest
                    ? new JsonResult(new { redirectUrl = adminLoginUrl })
                    : new RedirectResult(adminLoginUrl);
                return;
            }

            CurrentUser = new CurrentUserContext
            {
                Id = 1,
                Role = UserRole.SystemAdmin,
                Permissions = RolePermissions.For(UserRole.SystemAdmin),
                PlanFeatures = PlanFeature.ManageAppointments | PlanFeature.ManagePatientRecords |
                               PlanFeature.BasicReports | PlanFeature.AdvancedReports | PlanFeature.MarketingTools |
                               PlanFeature.PrioritySupport | PlanFeature.OnlineBooking | PlanFeature.ManageStaff |
                               PlanFeature.ManageDoctors,
                HasActivePlan = true
            };

            ViewBag.CurrentUser = CurrentUser;
            if (profileLoaded) ViewBag.HeaderProfile = profile;

            await LoadNotificationsAsync(_notificationService);
            await base.OnActionExecutionAsync(context, next);
        }

        public async Task<IActionResult> Index(int days = 30)
        {
            if (days is not (7 or 30 or 90)) days = 30;
            ViewBag.Days = days;

            var toDate = DateTime.Today.AddDays(1);
            var fromDate = DateTime.Today.AddDays(-days);

            ViewBag.Stats = new List<StatCardDto>();
            ViewBag.HasError = false;
            ViewBag.RevenueTrendJson = "[]";
            ViewBag.ClinicsGrowthJson = "[]";
            ViewBag.SubscriptionsByPlanJson = "[]";
            ViewBag.UsersGrowthJson = "[]";
            ViewBag.AppointmentsSummaryJson = "[]";

            try
            {
                var stats = await _adminDashboardService.GetStatsAsync();
                ViewBag.Stats = new List<StatCardDto>
                {
                    new StatCardDto
                    {
                        Value = stats.VerificationRequestsCount.ToString("N0"),
                        Label = "طلبات التحقق المعلقة",
                        IconColor = "amber",
                        SvgPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"
                    },
                    new StatCardDto
                    {
                        Value = stats.ActiveClinicsCount.ToString("N0"),
                        Label = "العيادات النشطة",
                        IconColor = "primary",
                        SvgPath = "M3 21h18v-2H3v2zM5 17h4a1 1 0 0 0 1-1V9a1 1 0 0 0-1-1H5a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1zm10 0h4a1 1 0 0 0 1-1V4a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1z"
                    },
                    new StatCardDto
                    {
                        Value = stats.TotalUsersCount.ToString("N0"),
                        Label = "إجمالي المستخدمين",
                        IconColor = "blue",
                        SvgPath = "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5s-3 1.34-3 3 1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05c1.16.84 1.97 2 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"
                    },
                    new StatCardDto
                    {
                        Value = stats.SpecializationsCount.ToString("N0"),
                        Label = "التخصصات الطبية",
                        IconColor = "green",
                        SvgPath = "M19 3h-4.18C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 0c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1zm2 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z"
                    },
                    new StatCardDto
                    {
                        Value = stats.ActiveAdsCount.ToString("N0"),
                        Label = "الإعلانات النشطة",
                        IconColor = "brass",
                        SvgPath = "M18 11v2h4v-2h-4zm-2 6.61c.96.71 2.21 1.65 3.2 2.39.4-.53.8-1.07 1.2-1.6-.99-.74-2.24-1.68-3.2-2.4-.4.54-.8 1.08-1.2 1.61zM20.4 5.6c-.4-.53-.8-1.07-1.2-1.6-.99.74-2.24 1.68-3.2 2.4.4.53.8 1.07 1.2 1.6.96-.72 2.21-1.65 3.2-2.4zM4 9c-1.1 0-2 .9-2 2v2c0 1.1.9 2 2 2h1v4h2v-4h1l5 3V6L8 9H4zm11.5 3c0-1.33-.58-2.53-1.5-3.35v6.69c.92-.81 1.5-2.01 1.5-3.34z"
                    },
                    new StatCardDto
                    {
                        Value = stats.RevokedSubscriptionsCount.ToString("N0"),
                        Label = "الاشتراكات الملغاة",
                        IconColor = "danger",
                        SvgPath = "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-2 12.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z"
                    }
                };
            }
            catch (ApiException ex)
            {
                ViewBag.HasError = true;
                ViewBag.ErrorMessage = ex.Message;
            }

            await LoadGraphDataAsync(days, fromDate, toDate);

            return View();
        }

        private async Task LoadGraphDataAsync(int days, DateTime fromDate, DateTime toDate)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            const string granularity = "day";

            try
            {
                var revenue = await _adminDashboardService.GetRevenueTrendAsync(granularity, fromDate, toDate);
                ViewBag.RevenueTrendJson = JsonSerializer.Serialize(revenue, jsonOptions);
            }
            catch (ApiException) { }

            try
            {
                var growth = await _adminDashboardService.GetClinicsGrowthAsync(granularity, fromDate, toDate);
                ViewBag.ClinicsGrowthJson = JsonSerializer.Serialize(growth, jsonOptions);
            }
            catch (ApiException) { }

            try
            {
                var byPlan = await _adminDashboardService.GetSubscriptionsByPlanAsync(fromDate, toDate);
                ViewBag.SubscriptionsByPlanJson = JsonSerializer.Serialize(byPlan, jsonOptions);
            }
            catch (ApiException) { }

            try
            {
                var users = await _adminDashboardService.GetUsersGrowthAsync(granularity, fromDate, toDate);
                ViewBag.UsersGrowthJson = JsonSerializer.Serialize(users, jsonOptions);
            }
            catch (ApiException) { }

            try
            {
                var appts = await _adminDashboardService.GetAppointmentsSummaryAsync(granularity, fromDate, toDate);
                ViewBag.AppointmentsSummaryJson = JsonSerializer.Serialize(appts, jsonOptions);
            }
            catch (ApiException) { }

            ViewBag.GraphsGranularity = granularity;
        }

        public async Task<IActionResult> Specializations(int pageNumber = 1, int pageSize = 20, bool? isFamous = null, bool? isActive = null)
        {
            try
            {
                var paged = await _specializationService.GetAllAsync(pageNumber, pageSize, isFamous, isActive);
                foreach (var s in paged.Items)
                {
                    if (!string.IsNullOrWhiteSpace(s.IconUrl) && !Uri.TryCreate(s.IconUrl, UriKind.Absolute, out _))
                    {
                        s.IconUrl = _attachmentUrlResolver.Resolve(s.IconUrl);
                    }
                }
                ViewBag.Specializations = paged.Items;
                ViewBag.Pagination = paged;
                ViewBag.CurrentFilter = isFamous;
                ViewBag.CurrentActiveFilter = isActive;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Specializations = new List<SpecializationDto>();
            }
            return View();
        }

        [Route("Admin/Specializations/{id:guid}")]
        public async Task<IActionResult> SpecializationDetail(Guid id)
        {
            try
            {
                var response = await _specializationService.GetByIdAsync(id);
                var spec = response?.Data;
                if (spec == null) return RedirectToAction("Specializations");

                if (!string.IsNullOrWhiteSpace(spec.IconUrl) && !Uri.TryCreate(spec.IconUrl, UriKind.Absolute, out _))
                {
                    spec.IconUrl = _attachmentUrlResolver.Resolve(spec.IconUrl);
                }
                ViewBag.Specialization = spec;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSpecialization([FromForm] CreateSpecializationRequest request)
        {
            try
            {
                await _specializationService.CreateAsync(request);
                TempData["SuccessMessage"] = "تم إضافة التخصص بنجاح";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Specializations");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSpecialization(Guid id,[FromForm] UpdateSpecializationRequest request)
        {
            try
            {
                request.Id = id;
                await _specializationService.UpdateAsync(request);
                TempData["SuccessMessage"] = "تم تحديث التخصص بنجاح";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Specializations");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSpecialization(Guid id)
        {
            try
            {
                var msg = await _specializationService.DeleteAsync(new DeleteSpecializationRequest(id));
                TempData["SuccessMessage"] = msg;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Specializations");
        }

        public async Task<IActionResult> Clinics(
            int pageNumber = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? status = null,
            string? name = null,
            string? email = null,
            string? phone = null,
            DateTime? createdFrom = null,
            DateTime? createdTo = null,
            string? sortBy = null,
            bool sortAscending = false,
            string? format = null)
        {
            ViewBag.GoogleMapsApiKey = _googleMapsOptions.Value.ApiKey;
            try
            {
                var request = new GetAllClinicsPagginatedRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    Status = status switch
                    {
                        "active" => ClinicStatus.Active,
                        "inactive" => ClinicStatus.Inactive,
                        _ => null
                    },
                    Name = name,
                    Email = email,
                    Phone = phone,
                    CreatedFrom = createdFrom,
                    CreatedTo = createdTo,
                    SortBy = sortBy,
                    SortAscending = sortAscending
                };
                var paged = await _clinicService.GetAllClinicsPaginatedAsync(request);
                if (paged?.Items != null)
                    foreach (var c in paged.Items)
                    {
                        c.Logo = _attachmentUrlResolver.Resolve(c.Logo);
                        c.ImageUrl = c.Logo;
                    }

                if (format == "json")
                    return Json(new { items = paged?.Items ?? new List<ClinicManagmentDto>(), hasMore = paged?.HasNextPage ?? false });

                ViewBag.Clinics = paged?.Items ?? new List<ClinicManagmentDto>();
                ViewBag.Pagination = paged;
            }
            catch (ApiException ex)
            {
                if (format == "json")
                    return Json(new { error = ex.Message, items = new List<ClinicManagmentDto>(), hasMore = false });

                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Clinics = new List<ClinicManagmentDto>();
            }

            try
            {
                var specs = await _specializationService.GetAllAsync(pageNumber: 1, pageSize: 200, isActive: true);
                ViewBag.Specializations = specs.Items.Where(s => s.IsActive).ToList();
            }
            catch (ApiException)
            {
                ViewBag.Specializations = new List<SpecializationDto>();
            }

            return View();
        }

        [Route("Admin/Clinics/Details/{id}")]
        public async Task<IActionResult> ClinicDetails(string id)
        {
            var realId = IdProtector.UnprotectGuid(id);
            ViewBag.GoogleMapsApiKey = _googleMapsOptions.Value.ApiKey;

            try
            {
                var details = await _clinicService.GetClinicDetailsAsync(new GetClinicByIdRequest { Id = realId });
                var clinic = details?.Data;
                if (clinic != null)
                {
                    clinic.Logo = _attachmentUrlResolver.Resolve(clinic.Logo);
                    ViewBag.Clinic = clinic;
                    if (clinic.Doctors != null)
                    {
                        foreach (var doc in clinic.Doctors)
                            doc.Image = _attachmentUrlResolver.Resolve(doc.Image);
                    }
                }
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                try
                {
                    var fallback = await _clinicService.GetClinicByIdAsync(new GetClinicByIdRequest { Id = realId });
                    var fallbackClinic = fallback?.Data;
                    if (fallbackClinic != null)
                    {
                        fallbackClinic.Logo = _attachmentUrlResolver.Resolve(fallbackClinic.Logo);
                        ViewBag.Clinic = fallbackClinic;
                    }
                }
                catch (ApiException)
                {
                    ViewBag.ErrorMessage = "العيادة غير موجودة";
                    ViewBag.Clinic = null;
                }
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Clinic = null;
            }

            try
            {
                var specs = await _specializationService.GetAllAsync(pageNumber: 1, pageSize: 200, isActive: true);
                ViewBag.Specializations = specs.Items.Where(s => s.IsActive).ToList();
            }
            catch (ApiException)
            {
                ViewBag.Specializations = new List<SpecializationDto>();
            }

            return View("ClinicDetails");
        }

        [HttpPost]
        public async Task<IActionResult> CreateClinic([FromBody] CreateClinicRequest request)
        {
            try
            {
                if (request == null)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    var msg = errors.Count > 0
                        ? "خطأ في البيانات: " + string.Join(" | ", errors)
                        : "البيانات مطلوبة";
                    return Json(new { success = false, error = msg });
                }

                var result = await _clinicService.CreateClinicAsync(request);
                if (result.Success)
                    return Json(new { success = true, message = "تم إنشاء العيادة بنجاح", data = result.Data });
                return Json(new { success = false, error = result.Message ?? "فشل إنشاء العيادة" });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateClinic(Guid id, [FromBody] UpdateClinicRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, error = "البيانات مطلوبة" });

                request.Id = id;
                var result = await _clinicService.UpdateClinicAsync(request);
                if (result.Success)
                    return Json(new { success = true, message = "تم تحديث العيادة بنجاح", data = result.Data });
                return Json(new { success = false, error = result.Message ?? "فشل تحديث العيادة" });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ActivateClinic(Guid id)
        {
            try
            {
                var result = await _clinicService.ActivateClinicAsync(new ActivateClinicRequest { Id = id });
                if (result.Success)
                    return Json(new { success = true, message = "تم تفعيل العيادة بنجاح", data = result.Data });
                return Json(new { success = false, error = result.Message ?? "فشل تفعيل العيادة" });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateClinic(Guid id)
        {
            try
            {
                var result = await _clinicService.DeactivateClinicAsync(new DeactivateClinicRequest { Id = id });
                if (result.Success)
                    return Json(new { success = true, message = "تم إلغاء تفعيل العيادة بنجاح", data = result.Data });
                return Json(new { success = false, error = result.Message ?? "فشل إلغاء تفعيل العيادة" });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadClinicImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Json(new { success = false, error = "الملف مطلوب" });

                var uploadRequest = new UploadAttachmentRequest(file, 5, MediaType.Image);
                var url = await _attachmentService.UploadAttachmentAsync(uploadRequest);
                if (string.IsNullOrWhiteSpace(url))
                {
                    return Json(new { success = false, error = "فشل رفع الملف أو لم يتم استرجاع المسار بنجاح" });
                }
                return Json(new { success = true, url, fileName = url });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        public async Task<IActionResult> Doctors(Guid? clinicId = null, int pageNumber = 1, int pageSize = 20, string? searchTerm = null, bool? isUnassigned = null, string? userTypes = null)
        {
            try
            {
                var request = new GetAllDoctorsRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    IsUnassigned = isUnassigned,
                    ClinicId = clinicId,
                    UserTypes = ParseUserTypes(userTypes)
                };
                var paged = await _doctorService.GetAllDoctorsPagginatedAsync(request);
                ViewBag.Doctors = paged.Items;
                ViewBag.Pagination = paged;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Doctors = new List<UserResponseDto>();
            }

            try
            {
                var clinicsResponse = await _clinicService.GetAllClinicsForViewingOnlyAsync(new GetAllCLinicsForViewingOnly());
                ViewBag.Clinics = clinicsResponse?.Data ?? new List<ClinicLookupDto>();
            }
            catch (ApiException)
            {
                ViewBag.Clinics = new List<ClinicLookupDto>();
            }

            ViewBag.SelectedClinicId = clinicId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsUnassigned = isUnassigned;
            ViewBag.SelectedUserTypes = userTypes;
            return View();
        }

        [Route("Admin/Doctors/Details/{id}")]
        public async Task<IActionResult> DoctorDetails(string id)
        {
            var realId = IdProtector.UnprotectGuid(id);
            ViewBag.Doctor = (DoctorDto?)null;
            try
            {
                ViewBag.Doctor = await _clinicDoctorService.GetDoctorByIdAsync(realId);
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] ??= ex.Message;
            }
            return View("DoctorDetails");
        }

        [Route("Admin/Verification")]
        public async Task<IActionResult> VerificationCenter(int pageNumber = 1, int pageSize = 20)
        {
            ViewBag.Requests = await _getPendingVerifications(pageNumber, pageSize);
            return View("VerificationCenter");
        }

        [Route("Admin/Verification/List")]
        public async Task<IActionResult> VerificationList(int pageNumber = 1, int pageSize = 20)
        {
            return PartialView("_VerificationList", await _getPendingVerifications(pageNumber, pageSize));
        }

        private async Task<PagginatedResult<UserVerficationDto>> _getPendingVerifications(int pageNumber, int pageSize)
        {
            try
            {
                return await _userVerificationService.GetPendingVerificationsAsync(new GetPendingVerficationsRequest { PageNumber = pageNumber, PageSize = pageSize });
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return new PagginatedResult<UserVerficationDto>(new List<UserVerficationDto>(), 0);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AcceptVerification(Guid userId)
        {
            try
            {
                var result = await _userVerificationService.ApproveUserVerificationAsync(new ApproveUserVerficationRequest { UserId = userId });
                var msg = result.Success ? "تم قبول طلب التحقق بنجاح" : (result.Message ?? "فشل قبول طلب التحقق");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = result.Success, message = msg });
                }
                if (result.Success)
                    TempData["SuccessMessage"] = msg;
                else
                    TempData["ErrorMessage"] = msg;
            }
            catch (ApiException ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = ex.Message });
                }
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("VerificationCenter");
        }

        [HttpPost]
        public async Task<IActionResult> RejectVerification(Guid userId, string? notes)
        {
            try
            {
                var result = await _userVerificationService.RejectUserVerificationAsync(new RejectUserVerificationRequest { UserId = userId, Notes = notes });
                var msg = result.Success ? "تم رفض طلب التحقق بنجاح" : (result.Message ?? "فشل رفض طلب التحقق");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = result.Success, message = msg });
                }
                if (result.Success)
                    TempData["SuccessMessage"] = msg;
                else
                    TempData["ErrorMessage"] = msg;
            }
            catch (ApiException ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = ex.Message });
                }
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("VerificationCenter");
        }

        [Route("Admin/Subscriptions")]
        public async Task<IActionResult> Subscriptions()
        {
            try
            {
                ViewBag.Plans = await _planService.GetAllAsync();
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Plans = new List<PlanDto>();
            }
            return View("Subscriptions");
        }

        [Route("Admin/PendingClinics")]
        public async Task<IActionResult> PendingClinics()
        {
            try
            {
                var result = await _userVerificationService.GetPendingVerificationsAsync(new GetPendingVerficationsRequest
                {
                    PageNumber = 1,
                    PageSize = 50
                });
                var items = result.Items ?? new List<UserVerficationDto>();
                ViewBag.PendingClinics = items.Where(r => r.RequestedRole == UserType.ClinicOwner).ToList();
            }
            catch (ApiException)
            {
                ViewBag.PendingClinics = new List<UserVerficationDto>();
            }
            return View("PendingClinics");
        }

        [Route("Admin/SubscriptionManagement")]
        public async Task<IActionResult> SubscriptionManagement(int? status = null, Guid? planId = null, Guid? clinicId = null, int pageNumber = 1, int pageSize = 20)
        {
            ViewBag.Clinics = new List<ClinicLookupDto>();
            try
            {
                var request = new GetPaginatedSubscriptionsRequest
                {
                    Status = status,
                    PlanId = planId,
                    ClinicId = clinicId,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var result = await _adminSubscriptionService.GetSubscriptionsAsync(request);
                ViewBag.Subscriptions = result.Items;
                ViewBag.Pagination = result;
                ViewBag.StatusFilter = status?.ToString();
                ViewBag.PlanIdFilter = planId;
                ViewBag.ClinicIdFilter = clinicId;
                ViewBag.Plans = await _adminSubscriptionService.GetAllPlansAsync();

                var clinicsResponse = await _clinicService.GetAllClinicsForViewingOnlyAsync(new GetAllCLinicsForViewingOnly());
                ViewBag.Clinics = clinicsResponse?.Data ?? new List<ClinicLookupDto>();
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Subscriptions = new List<SubscriptionDto>();
                ViewBag.Plans = new List<PlanDto>();
            }
            return View("SubscriptionManagement");
        }

        [HttpPost]
        [Route("Admin/SubscriptionManagement/Create")]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            try
            {
                if (request == null || request.ClinicId == Guid.Empty || request.PlanId == Guid.Empty)
                    return Json(new { success = false, message = "بيانات الاشتراك غير صالحة" });

                var subscription = await _adminSubscriptionService.CreateSubscriptionAsync(request);
                return Json(new { success = true, message = "تم إنشاء الاشتراك بنجاح", data = subscription });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        [Route("Admin/SubscriptionManagement/Revoke")]
        public async Task<IActionResult> RevokeSubscription([FromBody] RevokeSubscriptionRequest request)
        {
            try
            {
                var message = await _adminSubscriptionService.RevokeSubscriptionAsync(request);
                return Json(new { success = true, message });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Admin/PendingClinics/Approve")]
        public async Task<IActionResult> ApproveClinicRegistration([FromBody] ApproveClinicRequest request)
        {
            try
            {
                var userResult = await _userVerificationService.ApproveUserVerificationAsync(new ApproveUserVerficationRequest { UserId = request.ClinicId });
                try
                {
                    await _clinicService.ActivateClinicAsync(new ActivateClinicRequest { Id = request.ClinicId });
                }
                catch
                {
                    // Ignore clinic activation errors if already active or mapped differently
                }
                return Json(new { success = userResult.Success, message = userResult.Message });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Admin/PendingClinics/Reject")]
        public async Task<IActionResult> RejectClinicRegistration([FromBody] RejectClinicRequest request)
        {
            try
            {
                var result = await _userVerificationService.RejectUserVerificationAsync(new RejectUserVerificationRequest { UserId = request.ClinicId, Notes = request.Reason });
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Payments(
            int pageNumber = 1,
            int pageSize = 20,
            int? type = null,
            int? status = null,
            int? method = null,
            string? fromDate = null,
            string? toDate = null,
            string? searchTerm = null,
            string? month = null)
        {
            ViewBag.Stats = null;
            ViewBag.Payments = new List<AdminPaymentDto>();
            ViewBag.Clinics = new List<ClinicLookupDto>();
            ViewBag.EligibleClinics = new List<EligibleClinicDto>();
            ViewBag.AdPackages = new List<AdPackageDto>();

            if (string.IsNullOrWhiteSpace(month) || !DateTime.TryParseExact(month, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var selectedMonth))
            {
                selectedMonth = DateTime.Today;
                month = selectedMonth.ToString("yyyy-MM");
            }
            ViewBag.Month = month;
            ViewBag.MonthFilter = month;
            var statsFromDate = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
            var statsToDate = statsFromDate.AddMonths(1).AddDays(-1);

            try
            {
                var request = new GetAdminPaymentsRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Type = type,
                    Status = status,
                    Method = method,
                    FromDate = DateTime.TryParse(fromDate, out var fd) ? fd : null,
                    ToDate = DateTime.TryParse(toDate, out var td) ? td : null,
                    SearchTerm = searchTerm
                };
                var paged = await _adminPaymentService.GetPaymentsAsync(request);
                ViewBag.Payments = paged.Items;
                ViewBag.Pagination = paged;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Payments = new List<AdminPaymentDto>();
            }

            try
            {
                ViewBag.Stats = await _adminPaymentService.GetPaymentStatsAsync(statsFromDate, statsToDate);
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage ??= ex.Message;
                ViewBag.Stats = null;
            }

            try
            {
                var clinicsResponse = await _clinicService.GetAllClinicsForViewingOnlyAsync(new GetAllCLinicsForViewingOnly());
                ViewBag.Clinics = clinicsResponse?.Data ?? new List<ClinicLookupDto>();
            }
            catch (ApiException)
            {
                ViewBag.Clinics = new List<ClinicLookupDto>();
            }

            try
            {
                ViewBag.EligibleClinics = await _adminPaymentService.GetEligibleClinicsAsync() ?? new List<EligibleClinicDto>();
            }
            catch (ApiException)
            {
                ViewBag.EligibleClinics = new List<EligibleClinicDto>();
            }

            try
            {
                ViewBag.AdPackages = await _adminPaymentService.GetAdPackagesAsync() ?? new List<AdPackageDto>();
            }
            catch (ApiException)
            {
                ViewBag.AdPackages = new List<AdPackageDto>();
            }

            try
            {
                ViewBag.PlatformFee = (await _platformSettingService.GetSettingAsync())?.AppointmentFeePercent ?? 0m;
            }
            catch (ApiException)
            {
                ViewBag.PlatformFee = 0m;
            }

            ViewBag.TypeFilter = type?.ToString();
            ViewBag.StatusFilter = status?.ToString();
            ViewBag.MethodFilter = method?.ToString();
            ViewBag.FromDateFilter = fromDate;
            ViewBag.ToDateFilter = toDate;
            ViewBag.SearchTerm = searchTerm;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePlatformFee(decimal appointmentFeePercent)
        {
            if (appointmentFeePercent < 0 || appointmentFeePercent > 100)
            {
                TempData["Error"] = "نسبة رسوم المنصة يجب أن تكون بين 0 و 100.";
                return RedirectToAction(nameof(Payments));
            }

            try
            {
                var updated = await _platformSettingService.UpdateSettingAsync(appointmentFeePercent);
                TempData["Success"] = $"تم تحديث نسبة رسوم المنصة إلى {updated.AppointmentFeePercent}%";
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "تعذر تحديث نسبة رسوم المنصة";
            }

            return RedirectToAction(nameof(Payments));
        }

        [Route("Admin/PaymentsDetails/{id}")]
        public async Task<IActionResult> PaymentsDetails(string id)
        {
            var realId = IdProtector.UnprotectGuid(id);
            ViewBag.Detail = null;
            try
            {
                ViewBag.Detail = await _adminPaymentService.GetPaymentDetailAsync(realId);
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                ViewBag.ErrorMessage = "المعاملة غير موجودة";
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
            }
            return View("PaymentsDetails");
        }

        [HttpPost]
        public async Task<IActionResult> CreateManualPayment([FromBody] CreateManualPaymentRequest request)
        {
            try
            {
                if (request == null || request.PayerId == Guid.Empty || request.Amount <= 0)
                    return Json(new { success = false, message = "بيانات الدفعة غير صالحة" });

                var payment = await _adminPaymentService.CreateManualPaymentAsync(request);
                return Json(new { success = true, message = "تم تسجيل الدفعة بنجاح", data = payment });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentRequest request)
        {
            try
            {
                await _adminPaymentService.RefundPaymentAsync(id, request?.Reason);
                return Json(new { success = true, message = "تم استرداد المبلغ بنجاح" });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdsOrder([FromBody] CreateAdsOrderRequest request)
        {
            try
            {
                if (request == null || request.ClinicId == Guid.Empty || request.AdPackageId == Guid.Empty)
                    return Json(new { success = false, message = "بيانات طلب الإعلان غير صالحة" });

                if (string.IsNullOrWhiteSpace(request.ReturnUrl))
                    request.ReturnUrl = $"{Request.Scheme}://{Request.Host}/Home/PaymentResult?type=ads";

                var result = await _adminPaymentService.CreateAdsOrderAsync(request);
                return Json(new { success = true, message = "تم إنشاء طلب الإعلان بنجاح", data = result });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [Route("Admin/Ads")]
        public async Task<IActionResult> Ads(int pageNumber = 1, int pageSize = 20, int? status = null)
        {
            ViewBag.Ads = new List<AdDto>();
            ViewBag.Packages = new List<AdPackageDto>();

            try
            {
                var paged = await _adService.GetAdsAsync(pageNumber, pageSize, status);
                ViewBag.Ads = paged.Items;
                ViewBag.Pagination = paged;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
            }

            try
            {
                ViewBag.Packages = await _adService.GetAllPackagesAsync() ?? new List<AdPackageDto>();
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage ??= ex.Message;
            }

            ViewBag.StatusFilter = status?.ToString();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateAd(Guid id, [FromBody] JsonElement body)
        {
            try
            {
                var reason = body.TryGetProperty("reason", out var r) ? r.GetString() : null;
                await _adService.DeactivateAdAsync(id, reason);
                return Json(new { success = true, message = "تم إلغاء الإعلان بنجاح" });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdPackage([FromBody] UpsertAdPackageRequest request)
        {
            try
            {
                if (request == null || request.Price <= 0 || request.DurationDays <= 0)
                    return Json(new { success = false, message = "بيانات الباقة غير صالحة" });

                var result = await _adService.CreatePackageAsync(request);
                return Json(new { success = true, message = "تمت إضافة الباقة بنجاح", data = result });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAdPackage(Guid id, [FromBody] UpsertAdPackageRequest request)
        {
            try
            {
                if (request == null || request.Price <= 0 || request.DurationDays <= 0)
                    return Json(new { success = false, message = "بيانات الباقة غير صالحة" });

                var result = await _adService.UpdatePackageAsync(id, request);
                return Json(new { success = true, message = "تم تعديل الباقة بنجاح", data = result });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAdPackage(Guid id)
        {
            try
            {
                await _adService.DeletePackageAsync(id);
                return Json(new { success = true, message = "تم حذف الباقة بنجاح" });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [Route("Admin/Users")]
        public async Task<IActionResult> Users(int pageNumber = 1, int pageSize = 20, string? searchTerm = null, string? status = null, string? userTypes = null)
        {
            try
            {
                var request = new GetAllUsersRequest
                {
                    PageNumber = 1,
                    PageSize = 100,
                    SearchTerm = searchTerm,
                    UserTypes = ParseUserTypes(userTypes) ?? new()
                };
                var allUsers = await _userService.GetAllUsersPagginatedAsync(request);

                var users = allUsers.Items.Select(u => new UserListItemDto
                {
                    Id = u.Id,
                    Name = u.FullName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Initials = GetInitials(u.FullName),
                    Image = _attachmentUrlResolver.Resolve(u.Image ?? u.ImageUrl),
                    RegistrationDate = u.CreatedAt.ToString("d MMMM yyyy"),
                    Status = u.IsActive ? "نشط" : "غير نشط",
                    StatusClass = u.IsActive ? "badge-success" : "badge-warning",
                    Role = MapUserTypeToRole(u.Roles.FirstOrDefault()).ToString(),
                    Roles = u.Roles.Select(r => MapUserTypeToRole(r).ToString()).Where(r => r != UserRole.Patient.ToString()).ToList()
                }).ToList();

                if (!string.IsNullOrEmpty(status))
                {
                    users = users.Where(u => u.Status == status).ToList();
                }

                var filteredTotal = users.Count;
                var pageUsers = users.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.Users = pageUsers;
                ViewBag.Pagination = PagginatedResult<UserListItemDto>.Create(pageUsers, filteredTotal, pageNumber, pageSize);
                ViewBag.SearchTerm = searchTerm;
                ViewBag.StatusFilter = status;
                ViewBag.UserTypesFilter = userTypes;

            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Users = new List<UserListItemDto>();
            }

            try
            {
                var clinicsResponse = await _clinicService.GetAllClinicsForViewingOnlyAsync(new GetAllCLinicsForViewingOnly());
                ViewBag.Clinics = clinicsResponse?.Data ?? new List<ClinicLookupDto>();
            }
            catch (ApiException)
            {
                ViewBag.Clinics = new List<ClinicLookupDto>();
            }

            try
            {
                var specs = await _specializationService.GetAllAsync(pageNumber: 1, pageSize: 200, isActive: true);
                ViewBag.Specializations = specs.Items.Where(s => s.IsActive).ToList();
            }
            catch (ApiException)
            {
                ViewBag.Specializations = new List<SpecializationDto>();
            }

            return View("Users/Index");
        }

        private static string GetInitials(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "";
            var first = parts[0][0].ToString();
            var second = parts.Length > 1 && parts[1].Length > 1 ? parts[1][1].ToString() : "";
            return first + second;
        }

        private static UserRole MapUserTypeToRole(ClinicHub.Services.Enums.UserType? userType)
        {
            return userType switch
            {
                ClinicHub.Services.Enums.UserType.SuperAdmin => UserRole.SystemAdmin,
                ClinicHub.Services.Enums.UserType.ClinicOwner => UserRole.ClinicOwner,
                ClinicHub.Services.Enums.UserType.Doctor => UserRole.Doctor,
                ClinicHub.Services.Enums.UserType.Staff => UserRole.ClinicStaff,
                ClinicHub.Services.Enums.UserType.User => UserRole.Patient,
                _ => UserRole.Patient
            };
        }

        [HttpPost]
        [Route("Admin/Users/ChangePassword")]
        public async Task<IActionResult> ChangePassword(Guid id, string newPassword, string confirmPassword)
        {
            try
            {
                var request = new ChangePasswordRequest
                {
                    Id = id,
                    NewPassword = newPassword,
                    ConfirmPassword = confirmPassword
                };
                await _userService.ChangePasswordAsync(request);
                TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        [Route("Admin/Users/Create")]
        public async Task<IActionResult> CreateUser([FromForm] CreateUserRequest request, [FromForm] string? availabilitiesJson)
        {
            try
            {
                await _userService.CreateUserAsync(request);
                TempData["SuccessMessage"] = "تم إضافة المستخدم بنجاح";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        [Route("Admin/Users/Edit")]
        public async Task<IActionResult> EditUser(Guid id, string fullName, string phoneNumber, DateTime? birthDate, int? gender, bool? isActive)
        {
            try
            {
                var request = new EditUserRequest
                {
                    Id = id,
                    FullName = fullName,
                    PhoneNumber = phoneNumber,
                    BirthDate = birthDate?.ToString("yyyy-MM-dd"),
                    Gender = gender.HasValue ? (Gender)gender.Value : null,
                    IsActive = isActive
                };
                var result = await _userService.EditUserAsync(request);
                if (result.Success)
                    TempData["SuccessMessage"] = "تم تعديل المستخدم بنجاح";
                else
                    TempData["ErrorMessage"] = result.Message;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Users");
        }

        [Route("Admin/Users/Overview/{id}")]
        public async Task<IActionResult> UsersOverview(string id)
        {
            var realId = IdProtector.UnprotectGuid(id);
            ViewBag.User = await LoadUserOverviewAsync(realId);
            return View("Users/Overview");
        }

        [Route("Admin/Users/Visits/{id}")]
        public async Task<IActionResult> UsersVisits(string id)
        {
            var realId = IdProtector.UnprotectGuid(id);
            var overview = await LoadUserOverviewAsync(realId);
            ViewBag.UserId = realId;
            ViewBag.User = overview;
            ViewBag.Visits = overview.RecentVisits;
            return View("Users/Visits");
        }

        [Route("Admin/Users/Requests/{id}")]
        public async Task<IActionResult> UsersRequests(string id)
        {
            var realId = IdProtector.UnprotectGuid(id);
            var overview = await LoadUserOverviewAsync(realId);
            ViewBag.UserId = realId;
            ViewBag.User = overview;
            ViewBag.Requests = overview.Requests;
            return View("Users/Requests");
        }

        private async Task<AdminUserOverviewDto> LoadUserOverviewAsync(Guid id)
        {
            try
            {
                return await _adminDashboardService.GetUserOverviewAsync(id) ?? new AdminUserOverviewDto();
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] ??= ex.Message;
                return new AdminUserOverviewDto();
            }
        }

        [HttpPost]
        [Route("Admin/Users/Delete")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(new DeleteUserRequest { Id = id });
                if (result.Success)
                    TempData["SuccessMessage"] = "تم حذف المستخدم بنجاح";
                else
                    TempData["ErrorMessage"] = result.Message;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        [Route("Admin/Doctors/Delete")]
        public async Task<IActionResult> DeleteDoctor(Guid id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(new DeleteUserRequest { Id = id });
                if (result.Success)
                    TempData["SuccessMessage"] = "تم حذف الطبيب بنجاح";
                else
                TempData["ErrorMessage"] = result.Message;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Doctors");
        }

        [HttpPost]
        [Route("Admin/Doctors/ChangePassword")]
        public async Task<IActionResult> ChangePasswordDoctor(Guid id, string newPassword, string confirmPassword)
        {
            try
            {
                var request = new ChangePasswordRequest
                {
                    Id = id,
                    NewPassword = newPassword,
                    ConfirmPassword = confirmPassword
                };
                await _userService.ChangePasswordAsync(request);
                TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Doctors");
        }

        [HttpPost]
        [Route("Admin/Doctors/Edit")]
        public async Task<IActionResult> EditDoctor(Guid id, string fullName, string phoneNumber, DateTime? birthDate, int? gender, bool? isActive)
        {
            try
            {
                var request = new EditUserRequest
                {
                    Id = id,
                    FullName = fullName,
                    PhoneNumber = phoneNumber,
                    BirthDate = birthDate?.ToString("yyyy-MM-dd"),
                    Gender = gender.HasValue ? (Gender)gender.Value : null,
                    IsActive = isActive
                };
                var result = await _userService.EditUserAsync(request);
                if (result.Success)
                    TempData["SuccessMessage"] = "تم تعديل الطبيب بنجاح";
                else
                    TempData["ErrorMessage"] = result.Message;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Doctors");
        }

        [Route("Admin/Users/Payments/{id}")]
        public async Task<IActionResult> UsersPayments(string id)
        {
            var realId = IdProtector.UnprotectGuid(id);
            var overview = await LoadUserOverviewAsync(realId);
            ViewBag.UserId = realId;
            ViewBag.User = overview;
            ViewBag.Payments = overview.Payments;
            return View("Users/Payments");
        }

        public async Task<IActionResult> Notifications(int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var result = await _notificationService.GetNotificationsAsync(pageNumber, pageSize);
                ViewBag.Notifications = result.Items;
                ViewBag.Pagination = result;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Notifications = new List<NotificationDto>();
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "عذراً، حدث خطأ أثناء تحميل الإشعارات.";
                ViewBag.Notifications = new List<NotificationDto>();
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> NotificationsCount()
        {
            try
            {
                var count = await _notificationService.GetUnreadCountAsync();
                return Json(new { success = true, count });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, count = 0, message = ex.Message });
            }
            catch (Exception)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, count = 0, message = "عذراً، حدث خطأ أثناء جلب عدد الإشعارات." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> NotificationsList(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = await _notificationService.GetNotificationsAsync(pageNumber, pageSize);
                return Json(new
                {
                    success = true,
                    items = result.Items,
                    pageNumber = result.PageNumber,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages,
                    totalCount = result.TotalCount,
                    hasPreviousPage = result.HasPreviousPage,
                    hasNextPage = result.HasNextPage
                });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, items = new List<NotificationDto>() });
            }
            catch (Exception)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, items = new List<NotificationDto>() });
            }
        }

        public async Task<IActionResult> Profile()
        {
            try
            {
                ViewBag.Profile = await _authService.GetProfileAsync();
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"حدث خطأ غير متوقع: {ex.Message}";
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile()
        {
            try
            {
                string? profileImageUrl = null;
                var file = Request.Form.Files.GetFile("imageFile");
                if (file != null && file.Length > 0)
                {
                    profileImageUrl = await _attachmentService.UploadAttachmentAsync(new UploadAttachmentRequest(file, 1, MediaType.Image));
                }

                var fullName = Request.Form["fullName"].ToString();
                var phoneNumber = Request.Form["phoneNumber"].ToString();
                var birthDateText = Request.Form["birthDate"].ToString();
                var genderText = Request.Form["gender"].ToString();

                var request = new UpdateProfileRequest
                {
                    FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                    PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
                    BirthDate = DateTime.TryParseExact(birthDateText, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var birthDate) ? birthDate.ToString("yyyy-MM-dd") : null,
                    Gender = int.TryParse(genderText, out var gender) ? gender : null,
                    ProfileImageUrl = profileImageUrl
                };

                var success = await _authService.UpdateProfileAsync(request);
                return Json(new { success, message = success ? "تم تحديث الملف الشخصي بنجاح" : "فشل تحديث الملف الشخصي" });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        public async Task<IActionResult> PlanManagement()
        {
            try
            {
                var plans = await _planService.GetAllAsync();
                ViewBag.Plans = plans;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Plans = new List<PlanDto>();
            }

            return View();
        }

        private static List<UserType>? ParseUserTypes(string? userTypes)
        {
            if (string.IsNullOrWhiteSpace(userTypes))
                return null;
            var types = new List<UserType>();
            foreach (var part in userTypes.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out var val) && Enum.IsDefined(typeof(UserType), val))
                    types.Add((UserType)val);
            }
            return types.Count > 0 ? types : null;
        }
    }
}
