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

        public AdminController(ISpecializationService specializationService, IAttachmentUrlResolver attachmentUrlResolver, IUserVerificationService userVerificationService, IUserService userService, IDoctorService doctorService, IClinicService clinicService, IAttachmentService attachmentService, IOptions<GoogleMapsOptions> googleMapsOptions, IPlanService planService, IAdminSubscriptionService adminSubscriptionService, IAdminDashboardService adminDashboardService, IAuthService authService, IAdminPaymentService adminPaymentService, IAdService adService, INotificationService notificationService)
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
                TempData["ErrorMessage"] = "هذه الصفحة مخصصة لحسابات المشرف العام فقط.";
                context.Result = new RedirectResult(HomeRoutes.Pages.Index());
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

        public async Task<IActionResult> Index()
        {
            ViewBag.Stats = new List<MockStat>();
            ViewBag.UrgentTickets = new List<SupportTicketDto>();
            ViewBag.Subscribers = new List<SubscriptionDto>();
            ViewBag.HasError = false;

            try
            {
                var stats = await _adminDashboardService.GetStatsAsync();
                ViewBag.Stats = stats;
            }
            catch (ApiException ex)
            {
                ViewBag.HasError = true;
                ViewBag.ErrorMessage = ex.Message;
            }

            try
            {
                ViewBag.UrgentTickets = await _adminDashboardService.GetUrgentTicketsAsync() ?? new List<SupportTicketDto>();
            }
            catch (ApiException)
            {
                ViewBag.UrgentTickets = new List<SupportTicketDto>();
            }

            try
            {
                var subs = await _adminDashboardService.GetSubscriptionsAsync(pageNumber: 1, pageSize: 5);
                ViewBag.Subscribers = subs?.Items?.ToList() ?? new List<SubscriptionDto>();
            }
            catch (ApiException)
            {
                ViewBag.Subscribers = new List<SubscriptionDto>();
            }

            return View();
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
        public async Task<IActionResult> ClinicDetails(Guid id)
        {
            ViewBag.GoogleMapsApiKey = _googleMapsOptions.Value.ApiKey;

            try
            {
                var details = await _clinicService.GetClinicDetailsAsync(new GetClinicByIdRequest { Id = id });
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
                    var fallback = await _clinicService.GetClinicByIdAsync(new GetClinicByIdRequest { Id = id });
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
        public IActionResult DoctorDetails(Guid id)
        {
            ViewBag.Doctor = MockData.GetDoctorById(id);
            ViewBag.Clinics = MockData.GetClinics();
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

        public async Task<IActionResult> Support(int? status = null, int? priority = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var paged = await _adminDashboardService.GetTicketsAsync(status, priority, pageNumber, pageSize);
                ViewBag.Tickets = paged?.Items?.ToList() ?? new List<SupportTicketDto>();
                ViewBag.Pagination = paged;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Tickets = new List<SupportTicketDto>();
                ViewBag.Pagination = null;
            }

            ViewBag.StatusFilter = status?.ToString();
            ViewBag.PriorityFilter = priority?.ToString();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTicketStatus(Guid id, int status)
        {
            try
            {
                await _adminDashboardService.UpdateTicketStatusAsync(id, status);
                return Json(new { success = true, message = "تم تحديث حالة التذكرة بنجاح" });
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

            ViewBag.TypeFilter = type?.ToString();
            ViewBag.StatusFilter = status?.ToString();
            ViewBag.MethodFilter = method?.ToString();
            ViewBag.FromDateFilter = fromDate;
            ViewBag.ToDateFilter = toDate;
            ViewBag.SearchTerm = searchTerm;
            return View();
        }

        [Route("Admin/PaymentsDetails/{id:guid}")]
        public async Task<IActionResult> PaymentsDetails(Guid id)
        {
            ViewBag.Detail = null;
            try
            {
                ViewBag.Detail = await _adminPaymentService.GetPaymentDetailAsync(id);
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

                var users = allUsers.Items.Select(u => new MockUser
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
                    Role = MapUserTypeToRole(u.Roles.FirstOrDefault()),
                    Roles = u.Roles.Select(r => MapUserTypeToRole(r)).Where(r => r != UserRole.Patient).ToList(),
                    TotalVisits = 0,
                    AvgRating = 0,
                    TotalSpent = "0"
                }).ToList();

                if (!string.IsNullOrEmpty(status))
                {
                    users = users.Where(u => u.Status == status).ToList();
                }

                var filteredTotal = users.Count;
                var pageUsers = users.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.Users = pageUsers;
                ViewBag.Pagination = PagginatedResult<MockUser>.Create(pageUsers, filteredTotal, pageNumber, pageSize);
                ViewBag.SearchTerm = searchTerm;
                ViewBag.StatusFilter = status;
                ViewBag.UserTypesFilter = userTypes;

            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Users = new List<MockUser>();
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
        public IActionResult UsersOverview(int id)
        {
            var overview = MockData.GetUserOverview(id);
            overview.Image = _attachmentUrlResolver.Resolve(overview.Image);
            ViewBag.User = overview;
            return View("Users/Overview");
        }

        [Route("Admin/Users/Visits/{id}")]
        public IActionResult UsersVisits(int id)
        {
            ViewBag.UserId = id;
            ViewBag.Visits = MockData.GetUserVisits(id);
            return View("Users/Visits");
        }

        [Route("Admin/Users/Requests/{id}")]
        public IActionResult UsersRequests(int id)
        {
            ViewBag.UserId = id;
            ViewBag.Requests = MockData.GetUserRequests(id);
            return View("Users/Requests");
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
        public IActionResult UsersPayments(int id)
        {
            ViewBag.UserId = id;
            ViewBag.Payments = MockData.GetUserPayments(id);
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
