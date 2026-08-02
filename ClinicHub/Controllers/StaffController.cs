using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Enums;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Controllers
{
    public class StaffController : BaseController
    {
        private readonly IStaffDashboardService _staffDashboardService;
        private readonly IAuthService _authService;
        private readonly IAttachmentService _attachmentService;

        public StaffController(IStaffDashboardService staffDashboardService, IAuthService authService, IAttachmentService attachmentService)
        {
            _staffDashboardService = staffDashboardService;
            _authService = authService;
            _attachmentService = attachmentService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await LoadHeaderProfileAsync(_authService);
            await base.OnActionExecutionAsync(context, next);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            CurrentUser = new CurrentUserContext
            {
                Id = 5,
                ClinicId = MockData.ClinicId_Heart,
                Role = UserRole.ClinicStaff,
                Permissions = RolePermissions.For(UserRole.ClinicStaff),
                PlanFeatures = PlanFeature.ManageAppointments | PlanFeature.ManageStaff,
                HasActivePlan = true
            };
            base.OnActionExecuting(context);
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
                    BirthDate = DateTime.TryParse(birthDateText, out var birthDate) ? birthDate : null,
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

        public async Task<IActionResult> Index()
        {
            try
            {
                var statsTask = _staffDashboardService.GetStatsAsync();
                var queueTask = _staffDashboardService.GetQueueAsync();
                var pendingTask = _staffDashboardService.GetAppointmentsAsync("pending", null, null, 1, 1);

                await Task.WhenAll(statsTask, queueTask, pendingTask);

                var stats = statsTask.Result;
                var queue = queueTask.Result;

                ViewBag.Stats = stats;
                ViewBag.QueueItems = queue.Take(5).ToList();

                stats.Waiting = queue.Count(q =>
                    string.Equals(q.Status, "waiting", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(q.Status, "registered", StringComparison.OrdinalIgnoreCase));

                stats.PendingRequests = pendingTask.Result.TotalCount;

                // Load first 5 pending appointment requests for dashboard card
                var pendingFull = await _staffDashboardService.GetAppointmentsAsync("pending", null, null, 1, 5);
                ViewBag.PendingRequests = pendingFull.Items;
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ غير متوقع: {ex.Message}";
            }
            return View();
        }

        public async Task<IActionResult> Appointments(string? status, string? date, string? searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var data = await _staffDashboardService.GetAppointmentsAsync(status, date, searchTerm, pageNumber, pageSize);
                ViewBag.Appointments = data.Items;
                ViewBag.Pagination = data;
                ViewBag.StatusFilter = status;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.DateFilter = date;
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.Appointments = new List<StaffAppointmentDto>();
                ViewBag.Pagination = null;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ غير متوقع: {ex.Message}";
                ViewBag.Appointments = new List<StaffAppointmentDto>();
                ViewBag.Pagination = null;
            }
            return View();
        }

        public async Task<IActionResult> Queue(string? status, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var allQueue = await _staffDashboardService.GetQueueAsync();
                var filtered = string.IsNullOrWhiteSpace(status)
                    ? allQueue
                    : allQueue.Where(q => q.Status == status).ToList();
                var totalCount = filtered.Count;
                var totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
                if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

                var items = filtered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.QueueItems = items;
                ViewBag.Pagination = new
                {
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < totalPages
                };
                ViewBag.StatusFilter = status;

                var doctors = await _staffDashboardService.GetDoctorsAsync();
                ViewBag.Doctors = doctors;
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.QueueItems = new List<StaffQueueItemDto>();
                ViewBag.Pagination = null;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ غير متوقع: {ex.Message}";
                ViewBag.QueueItems = new List<StaffQueueItemDto>();
                ViewBag.Pagination = null;
            }
            return View();
        }
        public async Task<IActionResult> RegisterPatient()
        {
            try
            {
                var doctors = await _staffDashboardService.GetDoctorsAsync();
                ViewBag.Doctors = doctors;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Doctors = new List<StaffDoctorDto>();
            }
            catch (Exception)
            {
                ViewBag.Doctors = new List<StaffDoctorDto>();
            }
            return View();
        }

        public IActionResult DoctorSchedule(int doctorId)
        {
            ViewBag.DoctorId = doctorId;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var data = await _staffDashboardService.GetStatsAsync();
                return Json(new { success = true, data });
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

        [HttpGet]
        public async Task<IActionResult> GetQueue()
        {
            try
            {
                var data = await _staffDashboardService.GetQueueAsync();
                return Json(new { success = true, data });
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

        [HttpGet]
        public async Task<IActionResult> GetAppointments(string? status, string? date, string? patientName, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var data = await _staffDashboardService.GetAppointmentsAsync(status, date, patientName, pageNumber, pageSize);
                return Json(new { success = true, data });
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
        public async Task<IActionResult> ApproveAppointment(string id)
        {
            try
            {
                var result = await _staffDashboardService.ApproveAppointmentAsync(id);
                return Json(new { success = true, data = result, message = "تم قبول الحجز وتم إرسال رابط الدفع للمريض" });
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
        public async Task<IActionResult> RejectAppointment(string id, [FromBody] RejectRequest? body)
        {
            try
            {
                var result = await _staffDashboardService.RejectAppointmentAsync(id, body?.Reason);
                return Json(new { success = true, data = result, message = "تم رفض الموعد بنجاح" });
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
        public async Task<IActionResult> CheckIn(string id)
        {
            try
            {
                var result = await _staffDashboardService.CheckInPatientAsync(id);
                return Json(new { success = true, data = result, message = "تم تسجيل وصول المريض بنجاح" });
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
        public async Task<IActionResult> Complete(string id)
        {
            try
            {
                var result = await _staffDashboardService.CompleteAppointmentAsync(id);
                return Json(new { success = true, data = result, message = "تم إنهاء الكشف بنجاح" });
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
        public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientFromStaffRequest request)
        {
            try
            {
                var result = await _staffDashboardService.RegisterPatientAsync(request);
                return Json(new { success = true, data = result, message = result.Message });
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

        [HttpGet]
        public async Task<IActionResult> GetDoctors()
        {
            try
            {
                var data = await _staffDashboardService.GetDoctorsAsync();
                return Json(new { success = true, data });
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

        [HttpGet]
        public async Task<IActionResult> GetDoctorSchedule(string doctorId, string? date)
        {
            try
            {
                var data = await _staffDashboardService.GetDoctorScheduleAsync(doctorId, date);
                return Json(new { success = true, data });
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
    }

    public class RejectRequest
    {
        public string? Reason { get; set; }
    }
}
