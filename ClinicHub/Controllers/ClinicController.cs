using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Controllers
{
    public class ClinicController : BaseController
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPlanService _planService;

        public ClinicController(ISubscriptionService subscriptionService, IPlanService planService)
        {
            _subscriptionService = subscriptionService;
            _planService = planService;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            CurrentUser = new CurrentUserContext
            {
                Id = 6,
                Role = UserRole.ClinicOwner,
                Permissions = RolePermissions.For(UserRole.ClinicOwner)
            };
            base.OnActionExecuting(context);
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

        public IActionResult Staff()
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
