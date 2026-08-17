using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using ClinicHub.Data;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Enums;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Controllers
{
    public class ClinicController : BaseController
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPlanService _planService;
        private readonly IClinicDoctorService _clinicDoctorService;
        private readonly IClinicStaffService _clinicStaffService;
        private readonly ISpecializationService _specializationService;
        private readonly IUserService _userService;
        private readonly IAttachmentService _attachmentService;
        private readonly IClinicService _clinicService;
        private readonly IAuthService _authService;
        private readonly IOptions<GoogleMapsOptions> _googleMapsOptions;
        private readonly IAdService _adService;
        private readonly IClinicDashboardService _clinicDashboardService;
        private readonly IDoctorService _doctorService;
        private readonly IDoctorDashboardService _doctorDashboardService;
        private readonly INotificationService _notificationService;
        private readonly IRatingsService _ratingsService;

        public ClinicController(
            ISubscriptionService subscriptionService,
            IPlanService planService,
            IClinicDoctorService clinicDoctorService,
            IClinicStaffService clinicStaffService,
            ISpecializationService specializationService,
            IUserService userService,
            IAttachmentService attachmentService,
            IClinicService clinicService,
            IAuthService authService,
            IOptions<GoogleMapsOptions> googleMapsOptions,
            IAdService adService,
            IClinicDashboardService clinicDashboardService,
            IDoctorService doctorService,
            IDoctorDashboardService doctorDashboardService,
            INotificationService notificationService,
            IRatingsService ratingsService)
        {
            _subscriptionService = subscriptionService;
            _planService = planService;
            _clinicDoctorService = clinicDoctorService;
            _clinicStaffService = clinicStaffService;
            _specializationService = specializationService;
            _userService = userService;
            _attachmentService = attachmentService;
            _clinicService = clinicService;
            _authService = authService;
            _googleMapsOptions = googleMapsOptions;
            _adService = adService;
            _clinicDashboardService = clinicDashboardService;
            _doctorService = doctorService;
            _doctorDashboardService = doctorDashboardService;
            _notificationService = notificationService;
            _ratingsService = ratingsService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                var subscriptionTask = _subscriptionService.GetMySubscriptionAsync();
                var plansTask = _planService.GetAllAsync();

                await Task.WhenAll(subscriptionTask, plansTask);

                var subscription = subscriptionTask.Result;
                var plans = plansTask.Result;
                var plan = plans?.FirstOrDefault(p => p.Id == subscription.PlanId);

                bool isExpired = !subscription.IsActive || subscription.EndDate < DateTime.UtcNow;

                var featureStrings = plan != null
                    ? JsonSerializer.Deserialize<List<string>>(plan.Features) ?? new()
                    : new();

                CurrentUser = new CurrentUserContext
                {
                    Id = 6,
                    ClinicId = subscription.ClinicId,
                    Role = UserRole.ClinicOwner,
                    Permissions = RolePermissions.For(UserRole.ClinicOwner),
                    PlanId = subscription.PlanId.ToString(),
                    PlanName = subscription.PlanName,
                    PlanFeatures = PlanFeatureMap.FromFeatureStrings(featureStrings),
                    MaxDoctors = plan?.MaxDoctors,
                    MaxStaff = plan?.MaxStaff,
                    HasActivePlan = !isExpired
                };

                if (isExpired)
                {
                    string? action = context.RouteData.Values["action"]?.ToString()?.ToLower();
                    bool isSubscriptionAction = action is "mysubscription" or "subscribe" or "initiatepayment" or "cancelsubscription";

                    if (!isSubscriptionAction)
                    {
                        Response.StatusCode = 403;
                        context.Result = IsAjaxRequest
                            ? new JsonResult(new { success = false, message = "انتهت صلاحية الاشتراك. يرجى تجديد الاشتراك للمتابعة." })
                            : new ViewResult { ViewName = "Forbidden" };
                        ViewBag.CurrentUser = CurrentUser;
                        return;
                    }
                }
            }
            catch
            {
                CurrentUser = new CurrentUserContext
                {
                    Id = 6,
                    Role = UserRole.ClinicOwner,
                    Permissions = RolePermissions.For(UserRole.ClinicOwner),
                    HasActivePlan = false
                };
            }

            ViewBag.CurrentUser = CurrentUser;
            await LoadHeaderProfileAsync(_authService);
            await LoadNotificationsAsync(_notificationService);
            await next();
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.DashboardStats = await _clinicDashboardService.GetStatsAsync();
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.DashboardStats = null;
            }
            catch (Exception)
            {
                ViewBag.DashboardStats = null;
            }
            return View();
        }

        public async Task<IActionResult> DoctorAppointments(string? status, string? startDate, string? endDate, string? searchTerm, int pageNumber = 1, int pageSize = 10)
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

        public async Task<IActionResult> DoctorPatients(string? searchTerm, int pageNumber = 1, int pageSize = 10)
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

        public async Task<IActionResult> DoctorPatientHistory(Guid patientId, string? name, int pageNumber = 1, int pageSize = 10)
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

        public async Task<IActionResult> DoctorAvailability()
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
        public async Task<IActionResult> SaveDoctorAvailability([FromBody] JsonElement body)
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

        [HttpPut]
        public async Task<IActionResult> UpdateDoctorAppointmentStatus(Guid appointmentId, [FromBody] JsonElement body)
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

        public IActionResult AppointmentRevenue()
        {
            ViewBag.Revenues = MockData.GetAppointmentRevenues();
            return View();
        }

        public IActionResult MedicalRecords()
        {
            return View();
        }

        public IActionResult PatientPortal()
        {
            return View();
        }

        public async Task<IActionResult> Staff(int pageNumber = 1, int pageSize = 20, string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                var paged = await _clinicStaffService.GetStaffAsync(pageNumber, pageSize, searchTerm, isActive);
                ViewBag.StaffList = paged.Items;
                ViewBag.Pagination = paged;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.StaffList = new List<Services.ReponseModels.StaffDto>();
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.ActiveFilter = isActive;
            return View();
        }

        public async Task<IActionResult> Doctors(int pageNumber = 1, int pageSize = 20, string? searchTerm = null, Guid? specializationId = null)
        {
            try
            {
                var specs = await _specializationService.GetAllAsync(pageNumber: 1, pageSize: 200, isActive: true);
                ViewBag.Specializations = specs.Items;
            }
            catch
            {
                try
                {
                    var activeSpecs = await _specializationService.GetActiveAsync();
                    ViewBag.Specializations = activeSpecs;
                }
                catch
                {
                    ViewBag.Specializations = new List<Services.ReponseModels.SpecializationDto>();
                }
            }

            var clinicId = CurrentUser?.ClinicId;
            if (clinicId == null || clinicId == Guid.Empty)
            {
                ViewBag.ErrorMessage = "لم يتم العثور على العيادة المرتبطة بحسابك.";
                ViewBag.DoctorsList = new List<Services.ReponseModels.DoctorDto>();
                ViewBag.SearchTerm = searchTerm;
                ViewBag.SelectedSpecializationId = specializationId;
                return View();
            }

            try
            {
                var paged = await _clinicDoctorService.GetDoctorsAsync(clinicId.Value, pageNumber, pageSize, searchTerm, specializationId);
                ViewBag.DoctorsList = paged.Items;
                ViewBag.Pagination = paged;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.DoctorsList = new List<Services.ReponseModels.DoctorDto>();
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedSpecializationId = specializationId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateDoctor([FromBody] JsonElement body)
        {
            try
            {
                var clinicId = CurrentUser?.ClinicId;
                if (clinicId == null || clinicId == Guid.Empty)
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "لم يتم العثور على العيادة المرتبطة بحسابك." });
                }

                var specializationId = Guid.Parse(body.GetProperty("specializationId").GetString()!);
                var fullName = body.GetProperty("fullName").GetString()!;
                var email = body.GetProperty("email").GetString()!;
                var phoneNumber = body.GetProperty("phoneNumber").GetString()!;
                var password = body.GetProperty("password").GetString()!;
                var gender = body.GetProperty("gender").GetInt32();
                var yearsOfExperience = body.TryGetProperty("yearsOfExperience", out var expEl) ? expEl.GetInt32() : 0;
                var bio = body.TryGetProperty("bio", out var bioEl) ? bioEl.GetString() : null;
                var birthDate = body.TryGetProperty("birthDate", out var bdEl) ? bdEl.GetString() : null;
                var doctorImage = body.TryGetProperty("doctorImage", out var diEl) ? diEl.GetString() : null;

                var availabilities = new List<DoctorAvailabilityItem>();
                if (body.TryGetProperty("availabilities", out var availEl) && availEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in availEl.EnumerateArray())
                    {
                        availabilities.Add(new DoctorAvailabilityItem
                        {
                            DayOfWeek = item.GetProperty("dayOfWeek").GetInt32(),
                            StartTime = item.GetProperty("startTime").GetString()!,
                            EndTime = item.GetProperty("endTime").GetString()!,
                            SlotDurationMinutes = 30
                        });
                    }
                }

                var doctor = await _clinicDoctorService.CreateDoctorAsync(new CreateDoctorRequest
                {
                    ClinicId = clinicId.Value,
                    SpecializationId = specializationId,
                    FullName = fullName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Password = password,
                    Gender = gender,
                    BirthDate = birthDate,
                    Bio = bio,
                    YearsOfExperience = yearsOfExperience,
                    DoctorImage = doctorImage,
                    Availabilities = availabilities.Count > 0 ? availabilities : null
                });

                return Json(new { success = true, message = "تم إضافة الطبيب بنجاح", data = doctor });
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
        public async Task<IActionResult> GetDoctorById(Guid id)
        {
            try
            {
                var doctor = await _clinicDoctorService.GetDoctorByIdAsync(id);
                if (doctor == null)
                {
                    Response.StatusCode = 404;
                    return Json(new { success = false, message = "الطبيب غير موجود" });
                }
                return Json(new { success = true, data = doctor });
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
        public async Task<IActionResult> UpdateDoctor(Guid id, [FromBody] UpdateDoctorRequest request)
        {
            try
            {
                if (request.Availabilities != null)
                {
                    foreach (var a in request.Availabilities)
                        a.SlotDurationMinutes = 30;
                }
                var doctor = await _clinicDoctorService.UpdateDoctorAsync(id, request);
                return Json(new { success = true, message = "تم تحديث الطبيب بنجاح", data = doctor });
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
        public async Task<IActionResult> DeleteDoctor(Guid id)
        {
            try
            {
                var result = await _clinicDoctorService.DeleteDoctorAsync(id);
                return Json(new { success = result, message = result ? "تم حذف الطبيب بنجاح" : "فشل حذف الطبيب" });
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
        public async Task<IActionResult> GetStaffById(Guid id)
        {
            try
            {
                var staff = await _clinicStaffService.GetStaffByIdAsync(id);
                if (staff == null)
                {
                    Response.StatusCode = 404;
                    return Json(new { success = false, message = "الموظف غير موجود" });
                }
                return Json(new { success = true, data = staff });
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
        public async Task<IActionResult> CreateStaff()
        {
            try
            {
                var clinicId = CurrentUser?.ClinicId;
                if (clinicId == null || clinicId == Guid.Empty)
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "لم يتم العثور على العيادة المرتبطة بحسابك." });
                }

                var request = new CreateStaffRequest
                {
                    FullName = Request.Form["fullName"].FirstOrDefault() ?? string.Empty,
                    Email = Request.Form["email"].FirstOrDefault() ?? string.Empty,
                    PhoneNumber = Request.Form["phoneNumber"].FirstOrDefault() ?? string.Empty,
                    Password = Request.Form["password"].FirstOrDefault() ?? string.Empty,
                    ClinicId = clinicId.Value
                };

                var image = Request.Form["image"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(image))
                {
                    request.Image = image;
                }

                var id = await _clinicStaffService.CreateStaffAsync(request);
                return Json(new { success = true, message = "تم إضافة الموظف بنجاح", data = id });
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
        public async Task<IActionResult> UpdateStaff(Guid id)
        {
            try
            {
                var request = new UpdateStaffRequest
                {
                    FullName = Request.Form["fullName"].FirstOrDefault(),
                    PhoneNumber = Request.Form["phoneNumber"].FirstOrDefault(),
                    IsActive = bool.TryParse(Request.Form["isActive"].FirstOrDefault(), out var isActive) ? isActive : null
                };

                var removeImage = Request.Form["removeImage"].FirstOrDefault();
                var image = Request.Form["image"].FirstOrDefault();

                if (removeImage == "true")
                {
                    request.Image = string.Empty;
                }
                else if (!string.IsNullOrWhiteSpace(image))
                {
                    request.Image = image;
                }

                var result = await _clinicStaffService.UpdateStaffAsync(id, request);
                return Json(new { success = result, message = result ? "تم تحديث الموظف بنجاح" : "فشل تحديث الموظف" });
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
        public async Task<IActionResult> ChangeStaffPassword(Guid id, [FromBody] ChangePasswordRequest request)
        {
            try
            {
                var result = await _clinicStaffService.ChangeStaffPasswordAsync(id, request);
                return Json(new { success = true, data = result, message = "تم تغيير كلمة المرور بنجاح" });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, data = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, data = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeDoctorPassword(Guid id, [FromBody] ChangePasswordRequest request)
        {
            try
            {
                var result = await _clinicDoctorService.ChangeDoctorPasswordAsync(id, request);
                return Json(new { success = true, data = result, message = "تم تغيير كلمة المرور بنجاح" });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, data = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, data = false, message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStaff(Guid id)
        {
            try
            {
                var result = await _clinicStaffService.DeleteStaffAsync(id);
                return Json(new { success = result, message = result ? "تم حذف الموظف بنجاح" : "فشل حذف الموظف" });
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

        public IActionResult Reports()
        {
            return View();
        }

        public async Task<IActionResult> Marketing()
        {
            var clinicId = CurrentUser?.ClinicId ?? Guid.Empty;
            ViewBag.Ads = new List<AdDto>();
            ViewBag.Packages = new List<AdPackageDto>();

            try
            {
                ViewBag.Ads = await _adService.GetMyAdsAsync(clinicId) ?? new List<AdDto>();
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
            }

            try
            {
                ViewBag.Packages = await _adService.GetPackagesAsync() ?? new List<AdPackageDto>();
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage ??= ex.Message;
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdOrder([FromBody] CreateAdsOrderRequest request)
        {
            var clinicId = CurrentUser?.ClinicId ?? Guid.Empty;
            try
            {
                if (clinicId == Guid.Empty || request == null || request.AdPackageId == Guid.Empty)
                {
                    return Json(new { success = false, message = "بيانات طلب الإعلان غير صالحة" });
                }

                if (string.IsNullOrWhiteSpace(request.ReturnUrl))
                {
                    request.ReturnUrl = $"{Request.Scheme}://{Request.Host}/Clinic/AdPaymentResult";
                }

                var result = await _adService.CreateOrderAsync(clinicId, request);
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

        [HttpPost]
        public async Task<IActionResult> GetMyAdsJson()
        {
            var clinicId = CurrentUser?.ClinicId ?? Guid.Empty;
            try
            {
                if (clinicId == Guid.Empty)
                    return Json(new { success = false, message = "معرف العيادة غير متوفر" });

                var ads = await _adService.GetMyAdsAsync(clinicId) ?? new List<AdDto>();
                return Json(new { success = true, data = ads });
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

        public IActionResult AdPaymentResult(bool success = false)
        {
            ViewBag.PaymentSuccess = success;
            ViewBag.PaymentType = "ads";
            return View();
        }

        public IActionResult Support()
        {
            return View();
        }

        [Route("Clinic/MySubscription")]
        public async Task<IActionResult> MySubscription()
        {
            try
            {
                var subscription = await _subscriptionService.GetMySubscriptionAsync();
                ViewBag.Subscription = subscription;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Subscription = null;
            }

            try
            {
                ViewBag.Plans = await _planService.GetAllAsync();
            }
            catch (ApiException)
            {
                ViewBag.Plans = new List<Services.ReponseModels.PlanDto>();
            }

            return View("MySubscription");
        }

        [Route("Clinic/Subscribe")]
        public async Task<IActionResult> Subscribe(Guid planId, int period = 0)
        {
            if (!Request.Cookies.ContainsKey("AccessToken"))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Subscriptions", "Home") });
            }

            try
            {
                if (planId == Guid.Empty)
                {
                    var plans = await _planService.GetAllAsync();
                    var defaultPlan = plans?.FirstOrDefault(p => p.IsActive);
                    if (defaultPlan != null)
                    {
                        planId = defaultPlan.Id;
                    }
                }

                var returnUrl = $"{Request.Scheme}://{Request.Host}/Home/PaymentResult";

                var result = await _subscriptionService.InitiatePaymentAsync(new InitiatePaymentRequest
                {
                    PlanId = planId,
                    Period = period,
                    ReturnUrl = returnUrl
                });

                var targetUrl = result?.TargetRedirectUrl;
                if (string.IsNullOrWhiteSpace(targetUrl))
                {
                    return Redirect(Url.Action("PaymentResult", "Home", new { success = true }) ?? "/");
                }
                return Redirect(targetUrl);
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Redirect(Url.Action("PaymentResult", "Home", new { success = false }) ?? "/");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء البدء في عملية الدفع: " + ex.Message;
                return Redirect(Url.Action("PaymentResult", "Home", new { success = false }) ?? "/");
            }
        }



        [Route("Clinic/InitiatePayment")]
        [HttpPost]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ReturnUrl))
                {
                    request.ReturnUrl = $"{Request.Scheme}://{Request.Host}/Home/PaymentResult";
                }
                var result = await _subscriptionService.InitiatePaymentAsync(request);
                return Json(new { success = true, targetUrl = result?.TargetRedirectUrl });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء البدء في عملية الدفع: " + ex.Message });
            }
        }

        [Route("Clinic/CancelSubscription")]
        [HttpPost]
        public async Task<IActionResult> CancelSubscription()
        {
            try
            {
                var message = await _subscriptionService.CancelMySubscriptionAsync();
                return Json(new { success = true, message });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Settings()
        {
            ViewBag.GoogleMapsApiKey = _googleMapsOptions.Value.ApiKey;

            try
            {
                var settings = await _clinicService.GetClinicSettingsAsync();
                ViewBag.Settings = settings.Data;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Settings = null;
            }

            try
            {
                ViewBag.Specializations = await _specializationService.GetActiveAsync();
            }
            catch (ApiException)
            {
                ViewBag.Specializations = new List<SpecializationDto>();
            }

            // مدة الموعد ثابتة على 30 دقيقة لجميع الأطباء والأيام — لا تُدار من أوقات العمل
            ViewBag.TypicalSlotDuration = 30;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] JsonElement body)
        {
            try
            {
                var request = new UpdateClinicSettingsRequest
                {
                    Name = body.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty,
                    Description = body.TryGetProperty("description", out var descEl) ? descEl.GetString() : null,
                    Phone = body.TryGetProperty("phone", out var phoneEl) ? phoneEl.GetString() : null,
                    ManagerName = body.TryGetProperty("managerName", out var mnEl) ? mnEl.GetString() : null,
                    Location = body.TryGetProperty("location", out var locEl) ? locEl.GetString() : null,
                    SpecializationId = body.TryGetProperty("specializationId", out var specEl) ? Guid.Parse(specEl.GetString()!) : Guid.Empty,
                    ConsultationFee = body.TryGetProperty("consultationFee", out var feeEl) ? feeEl.GetDecimal() : 0,
                    Currency = body.TryGetProperty("currency", out var curEl) ? curEl.GetString() : null,
                    MaxAdvanceBookingDays = body.TryGetProperty("maxAdvanceBookingDays", out var maxDaysEl) ? maxDaysEl.GetInt32() : 0,
                    ReservationTtlMinutes = body.TryGetProperty("reservationTtlMinutes", out var ttlEl) ? ttlEl.GetInt32() : 0,
                    CancellationWindowMinutes = body.TryGetProperty("cancellationWindowMinutes", out var cancelEl) ? cancelEl.GetInt32() : 0,
                    Latitude = body.TryGetProperty("latitude", out var latEl) && latEl.ValueKind != JsonValueKind.Null ? latEl.GetDouble() : (double?)null,
                    Longitude = body.TryGetProperty("longitude", out var lngEl) && lngEl.ValueKind != JsonValueKind.Null ? lngEl.GetDouble() : (double?)null,
                    IsActive = !body.TryGetProperty("isActive", out var activeEl) || activeEl.GetBoolean()
                };

                var result = await _clinicService.UpdateClinicSettingsAsync(request);
                return Json(new { success = true, message = "تم حفظ إعدادات العيادة بنجاح", data = result.Data });
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

        public async Task<IActionResult> Ratings()
        {
            var ratings = new List<RatingDto>();
            var receptionRatings = new List<RatingDto>();
            var cleanlinessRatings = new List<RatingDto>();
            try
            {
                if (CurrentUser?.ClinicId != null)
                {
                    var clinicId = CurrentUser.ClinicId.Value;
                    ratings = await _ratingsService.GetClinicRatingsAsync(clinicId);
                    receptionRatings = await _ratingsService.GetReceptionRatingsAsync(clinicId);
                    cleanlinessRatings = await _ratingsService.GetPlaceCleanlinessRatingsAsync(clinicId);
                }

                ViewBag.Ratings = ratings;
                ViewBag.ReceptionRatings = receptionRatings;
                ViewBag.CleanlinessRatings = cleanlinessRatings;
                ViewBag.AverageRating = ratings.Count > 0 ? ratings.Average(r => r.Value) : 0;
                ViewBag.TotalRatings = ratings.Count;
                ViewBag.ReceptionAverage = receptionRatings.Count > 0 ? receptionRatings.Average(r => r.Value) : 0;
                ViewBag.TotalReceptionRatings = receptionRatings.Count;
                ViewBag.CleanlinessAverage = cleanlinessRatings.Count > 0 ? cleanlinessRatings.Average(r => r.Value) : 0;
                ViewBag.TotalCleanlinessRatings = cleanlinessRatings.Count;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "عذراً، حدث خطأ أثناء تحميل التقييمات.";
            }
            return View();
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
    }
}
