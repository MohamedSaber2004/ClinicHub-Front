using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Controllers
{
    public class StaffController : BaseController
    {
        private readonly IStaffDashboardService _staffDashboardService;

        public StaffController(IStaffDashboardService staffDashboardService)
        {
            _staffDashboardService = staffDashboardService;
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

        public async Task<IActionResult> Index()
        {
            try
            {
                var stats = await _staffDashboardService.GetStatsAsync();
                ViewBag.Stats = stats;

                var queue = await _staffDashboardService.GetQueueAsync();
                ViewBag.QueueItems = queue.Take(5).ToList();

                stats.Waiting = queue.Count(q =>
                    string.Equals(q.Status, "waiting", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(q.Status, "registered", StringComparison.OrdinalIgnoreCase));
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
        public IActionResult RegisterPatient() => View();

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
                return Json(new { success = true, data = result, message = "تم تأكيد الموعد بنجاح" });
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
