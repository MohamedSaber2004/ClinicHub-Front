using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;

namespace ClinicHub.Controllers
{
    public class ClinicController : BaseController
    {
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

        public IActionResult Settings()
        {
            var user = CurrentUser;
            var clinic = MockData.GetClinics().FirstOrDefault(c => c.OwnerUserId == user?.Id);
            ViewBag.Clinic = clinic ?? MockData.GetClinics().FirstOrDefault();
            return View();
        }
    }
}
