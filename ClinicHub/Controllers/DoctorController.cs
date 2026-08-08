using System.Text.Json;
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
    public class DoctorController : BaseController
    {
        private readonly IDoctorService _doctorService;
        private readonly IDoctorDashboardService _doctorDashboardService;
        private readonly IAuthService _authService;
        private readonly IAttachmentService _attachmentService;
        private readonly INotificationService _notificationService;

        public DoctorController(IDoctorService doctorService, IDoctorDashboardService doctorDashboardService, IAuthService authService, IAttachmentService attachmentService, INotificationService notificationService)
        {
            _doctorService = doctorService;
            _doctorDashboardService = doctorDashboardService;
            _authService = authService;
            _attachmentService = attachmentService;
            _notificationService = notificationService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await LoadHeaderProfileAsync(_authService);
            await LoadNotificationsAsync(_notificationService);
            await base.OnActionExecutionAsync(context, next);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            CurrentUser = new CurrentUserContext
            {
                Id = 2,
                ClinicId = MockData.ClinicId_Heart,
                Role = UserRole.Doctor,
                Permissions = RolePermissions.For(UserRole.Doctor),
                PlanFeatures = PlanFeature.ManageAppointments | PlanFeature.ManagePatientRecords,
                HasActivePlan = true
            };
            base.OnActionExecuting(context);
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
                ViewBag.Stats = await _doctorDashboardService.GetStatsAsync();
                ViewBag.RecentAppointments = await _doctorDashboardService.GetRecentAppointmentsAsync(5);
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

        public async Task<IActionResult> Appointments(string? status, string? startDate, string? endDate, string? searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var data = await _doctorDashboardService.GetAppointmentsAsync(
                    ParseStatus(status), searchTerm, startDate, endDate, pageNumber, pageSize);
                ViewBag.Appointments = data.Items;
                ViewBag.Pagination = data;
                ViewBag.StatusFilter = status;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.Appointments = new List<DoctorAppointmentDto>();
                ViewBag.Pagination = null;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ غير متوقع: {ex.Message}";
                ViewBag.Appointments = new List<DoctorAppointmentDto>();
                ViewBag.Pagination = null;
            }
            return View();
        }

        public async Task<IActionResult> Patients(string? searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var data = await _doctorDashboardService.GetPatientsAsync(searchTerm, pageNumber, pageSize);
                ViewBag.Patients = data.Items;
                ViewBag.Pagination = data;
                ViewBag.SearchTerm = searchTerm;
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.Patients = new List<DoctorPatientDto>();
                ViewBag.Pagination = null;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ غير متوقع: {ex.Message}";
                ViewBag.Patients = new List<DoctorPatientDto>();
                ViewBag.Pagination = null;
            }
            return View();
        }

        public async Task<IActionResult> PatientHistory(Guid patientId, string? name, int pageNumber = 1, int pageSize = 10)
        {
            ViewBag.PatientId = patientId;
            ViewBag.PatientName = string.IsNullOrWhiteSpace(name) ? "مريض" : name;

            try
            {
                var data = await _doctorDashboardService.GetPatientHistoryAsync(patientId, pageNumber, pageSize);
                ViewBag.History = data.Items;
                ViewBag.Pagination = data;
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.History = new List<PatientHistoryDto>();
                ViewBag.Pagination = null;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ غير متوقع: {ex.Message}";
                ViewBag.History = new List<PatientHistoryDto>();
                ViewBag.Pagination = null;
            }
            return View();
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAppointmentStatus(Guid appointmentId, [FromBody] JsonElement body)
        {
            try
            {
                var status = body.TryGetProperty("status", out var statusEl) && statusEl.ValueKind == JsonValueKind.Number
                    ? statusEl.GetInt32()
                    : 0;
                var notes = body.TryGetProperty("notes", out var notesEl) && notesEl.ValueKind == JsonValueKind.String
                    ? notesEl.GetString()
                    : null;

                var result = await _doctorDashboardService.UpdateStatusAsync(appointmentId, status, notes);
                var message = status switch
                {
                    6 => "تم قبول الحجز وتم إرسال رابط الدفع للمريض",
                    2 => "تم رفض الحجز",
                    3 => "تم إكمال الموعد",
                    5 => "تم تسجيل الحالة",
                    _ => "تم تحديث حالة الموعد بنجاح"
                };
                return Json(new { success = true, data = result, message });
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

        private static int? ParseStatus(string? status) =>
            int.TryParse(status, out var value) ? value : null;

        public async Task<IActionResult> Availability()
        {
            ViewBag.AvailabilityJson = "[]";
            ViewBag.Stats = new List<MockStat>();
            ViewBag.TypicalDuration = 30;

            try
            {
                var items = await _doctorService.GetMyAvailabilityAsync();
                if (items == null || items.Count == 0)
                    items = BuildDefaultAvailability();

                ViewBag.AvailabilityJson = JsonSerializer.Serialize(items);
                ViewBag.Stats = BuildAvailabilityStats(items);
                ViewBag.TypicalDuration = 30;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "عذراً، حدث خطأ. يرجى المحاولة لاحقاً.";
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveAvailability([FromBody] JsonElement body)
        {
            try
            {
                var days = new List<DoctorAvailabilityWeekItem>();
                if (body.TryGetProperty("days", out var daysEl) && daysEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in daysEl.EnumerateArray())
                    {
                        days.Add(new DoctorAvailabilityWeekItem
                        {
                            Id = item.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String && Guid.TryParse(idEl.GetString(), out var id) ? id : null,
                            DayOfWeek = item.GetProperty("dayOfWeek").GetInt32(),
                            StartTime = item.GetProperty("startTime").GetString()!,
                            EndTime = item.GetProperty("endTime").GetString()!,
                            SlotDurationMinutes = 30
                        });
                    }
                }

                var week = await _doctorService.ReplaceWeeklyAvailabilityAsync(new ReplaceWeeklyAvailabilityRequest { Days = days });
                return Json(new { success = true, message = "تم حفظ أوقات العمل المتاحة بنجاح", data = week });
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

        private static List<DoctorAvailabilityDto> BuildDefaultAvailability()
        {
            var defaults = new List<DoctorAvailabilityDto>();
            for (var day = 0; day <= 4; day++)
            {
                defaults.Add(new DoctorAvailabilityDto
                {
                    Id = Guid.Empty,
                    DayOfWeek = day,
                    StartTime = "09:00:00",
                    EndTime = "17:00:00",
                    SlotDurationMinutes = 30
                });
            }
            return defaults;
        }

        private static List<MockStat> BuildAvailabilityStats(List<DoctorAvailabilityDto> items)
        {
            var activeDays = items.Select(i => i.DayOfWeek).Distinct().Count();

            var totalHours = 0.0;
            foreach (var item in items)
            {
                if (TimeSpan.TryParse(item.StartTime, out var start) && TimeSpan.TryParse(item.EndTime, out var end))
                    totalHours += (end - start).TotalHours;
            }

            var typicalDuration = 30;

            return new()
            {
                new() { Value = activeDays.ToString(), Label = "أيام العمل الأسبوعية", IconColor = "green", SvgPath = "M19 3h-1V1h-2v2H8V1H6v2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V8h14v11zM9 10H7v2h2v-2zm4 0h-2v2h2v-2zm4 0h-2v2h2v-2zm-8 4H7v2h2v-2zm4 0h-2v2h2v-2zm4 0h-2v2h2v-2z" },
                new() { Value = totalHours.ToString("0.#"), Label = "ساعات العمل أسبوعياً", IconColor = "primary", SvgPath = "M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67V7z" },
                new() { Value = $"{typicalDuration} دقيقة", Label = "مدة الحجز (ثابتة)", IconColor = "amber", SvgPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z" },
            };
        }
    }
}
