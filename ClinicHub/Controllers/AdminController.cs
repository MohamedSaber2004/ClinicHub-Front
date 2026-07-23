using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;
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

        public AdminController(ISpecializationService specializationService, IAttachmentUrlResolver attachmentUrlResolver, IUserVerificationService userVerificationService, IUserService userService, IDoctorService doctorService, IClinicService clinicService, IAttachmentService attachmentService, IOptions<GoogleMapsOptions> googleMapsOptions, IPlanService planService, IAdminSubscriptionService adminSubscriptionService)
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
        }

        public IActionResult Index()
        {
            ViewBag.Stats = MockData.GetDashboardStats();
            ViewBag.QuickStatuses = MockData.GetQuickStatuses();
            ViewBag.DoctorsOnDuty = MockData.GetDoctorsOnDuty();
            ViewBag.UrgentTickets = MockData.GetUrgentTickets();
            ViewBag.Subscribers = MockData.GetSubscribers();
            ViewBag.ActivityLog = MockData.GetActivityLog();
            ViewBag.NewPatients = MockData.GetNewPatients();
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
                var response = await _clinicService.GetClinicByIdAsync(new GetClinicByIdRequest { Id = id });
                var clinic = response?.Data;
                if (clinic != null)
                {
                    clinic.Logo = _attachmentUrlResolver.Resolve(clinic.Logo);
                    clinic.ImageUrl = clinic.Logo;
                    ViewBag.Clinic = clinic;
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
            try
            {
                var paged = await _userVerificationService.GetPendingVerificationsAsync(new GetPendingVerficationsRequest { PageNumber = pageNumber, PageSize = pageSize });
                ViewBag.Requests = paged;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Requests = new PagginatedResult<UserVerficationDto>(new List<UserVerficationDto>(), 0);
            }

            return View("VerificationCenter");
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

        [Route("Admin/PlanManagement")]
        public async Task<IActionResult> PlanManagement()
        {
            try
            {
                ViewBag.Plans = await _adminSubscriptionService.GetAllPlansAsync();
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Plans = new List<PlanDto>();
            }
            return View("PlanManagement");
        }

        [Route("Admin/SubscriptionManagement")]
        public async Task<IActionResult> SubscriptionManagement(int? status = null, Guid? planId = null, Guid? clinicId = null, int pageNumber = 1, int pageSize = 20)
        {
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
                ViewBag.Plans = await _adminSubscriptionService.GetAllPlansAsync();
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
        [Route("Admin/PlanManagement/Create")]
        public async Task<IActionResult> CreatePlan([FromBody] PlanDto plan)
        {
            try
            {
                var result = await _adminSubscriptionService.CreatePlanAsync(plan);
                return Json(new { success = true, data = result });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Admin/PlanManagement/Update")]
        public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] PlanDto plan)
        {
            try
            {
                var result = await _adminSubscriptionService.UpdatePlanAsync(id, plan);
                return Json(new { success = true, data = result });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Admin/PlanManagement/Delete")]
        public async Task<IActionResult> DeletePlan([FromBody] DeleteRequest request)
        {
            try
            {
                var message = await _adminSubscriptionService.DeletePlanAsync(request.Id);
                return Json(new { success = true, message });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
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

        public IActionResult Support()
        {
            ViewBag.Tickets = MockData.GetSupportTickets();
            return View();
        }

        public IActionResult Payments()
        {
            ViewBag.Stats = MockData.GetPaymentStats();
            ViewBag.Payments = MockData.GetPayments();
            return View();
        }

        public IActionResult PaymentsDetails(int id)
        {
            ViewBag.Detail = MockData.GetPaymentDetail(id);
            return View("PaymentsDetails");
        }

        [Route("Admin/Users")]
        public async Task<IActionResult> Users(int pageNumber = 1, int pageSize = 20, string? searchTerm = null, string? status = null)
        {
            try
            {
                var request = new GetAllUsersRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm
                };
                var paged = await _userService.GetAllUsersPagginatedAsync(request);

                var users = paged.Items.Select(u => new MockUser
                {
                    Id = u.Id,
                    Name = u.FullName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Initials = GetInitials(u.FullName),
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

                ViewBag.Users = users;
                ViewBag.Pagination = paged;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.StatusFilter = status;

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
        public async Task<IActionResult> CreateUser([FromForm] CreateUserRequest request)
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
                    BirthDate = birthDate,
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
            ViewBag.User = MockData.GetUserOverview(id);
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
                    BirthDate = birthDate,
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

        public IActionResult Profile()
        {
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
