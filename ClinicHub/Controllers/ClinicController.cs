using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
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

        public ClinicController(
            ISubscriptionService subscriptionService,
            IPlanService planService,
            IClinicDoctorService clinicDoctorService,
            IClinicStaffService clinicStaffService,
            ISpecializationService specializationService,
            IUserService userService)
        {
            _subscriptionService = subscriptionService;
            _planService = planService;
            _clinicDoctorService = clinicDoctorService;
            _clinicStaffService = clinicStaffService;
            _specializationService = specializationService;
            _userService = userService;
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
                        TempData["ErrorMessage"] = "انتهت صلاحية الاشتراك. يرجى تجديد الاشتراك للمتابعة.";
                        context.Result = new RedirectToActionResult("MySubscription", "Clinic", null);
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
            await next();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Appointments()
        {
            return View();
        }

        public IActionResult MedicalRecords()
        {
            return View();
        }

        public IActionResult Billing()
        {
            return View();
        }

        public IActionResult Inventory()
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

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term, int pageSize = 20)
        {
            try
            {
                var request = new GetAllUsersRequest
                {
                    SearchTerm = term,
                    PageSize = pageSize,
                    IsUnassigned = true
                };
                var result = await _userService.GetAllUsersPagginatedAsync(request);
                return Json(new { success = true, items = result.Items.Select(u => new { u.Id, u.FullName, u.Email, u.PhoneNumber }) });
            }
            catch (Exception)
            {
                return Json(new { success = false, items = new List<object>() });
            }
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

                var userId = Guid.Parse(body.GetProperty("userId").GetString()!);
                var specializationId = Guid.Parse(body.GetProperty("specializationId").GetString()!);
                var yearsOfExperience = body.GetProperty("yearsOfExperience").GetInt32();
                var bio = body.TryGetProperty("bio", out var bioEl) ? bioEl.GetString() : null;

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
                            SlotDurationMinutes = item.TryGetProperty("slotDurationMinutes", out var slotEl) ? slotEl.GetInt32() : 30
                        });
                    }
                }

                var doctor = await _clinicDoctorService.CreateDoctorAsync(new CreateDoctorRequest
                {
                    ClinicId = clinicId.Value,
                    UserId = userId,
                    SpecializationId = specializationId,
                    Bio = bio,
                    YearsOfExperience = yearsOfExperience,
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
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
        {
            try
            {
                var clinicId = CurrentUser?.ClinicId;
                if (clinicId == null || clinicId == Guid.Empty)
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "لم يتم العثور على العيادة المرتبطة بحسابك." });
                }
                request.ClinicId = clinicId.Value;

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
        public async Task<IActionResult> UpdateStaff(Guid id, [FromBody] UpdateStaffRequest request)
        {
            try
            {
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

        public IActionResult OnlineBooking()
        {
            return View();
        }

        public IActionResult Marketing()
        {
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

        public IActionResult Settings()
        {
            var user = CurrentUser;
            var clinic = MockData.GetClinics().FirstOrDefault(c => c.OwnerUserId == user?.Id);
            ViewBag.Clinic = clinic ?? MockData.GetClinics().FirstOrDefault();
            return View();
        }
    }
}
