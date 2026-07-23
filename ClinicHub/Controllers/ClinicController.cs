using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;

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

        public IActionResult Settings()
        {
            var user = CurrentUser;
            var clinic = MockData.GetClinics().FirstOrDefault(c => c.OwnerUserId == user?.Id);
            ViewBag.Clinic = clinic ?? MockData.GetClinics().FirstOrDefault();
            return View();
        }
    }
}
