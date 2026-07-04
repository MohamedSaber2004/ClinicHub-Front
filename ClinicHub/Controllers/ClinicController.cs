using ClinicHub.Data;
using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.Controllers
{
    public class ClinicController : Controller
    {
        public ClinicController()
        {
            // TODO: Remove hardcoded role — temporary for development testing
            if (CurrentUserContext.Current == null)
            {
                CurrentUserContext.Current = new CurrentUserContext
                {
                    Id = 6,
                    Role = UserRole.ClinicOwner,
                    Permissions = RolePermissions.For(UserRole.ClinicOwner)
                };
            }
            ViewBag.CurrentUser = CurrentUserContext.Current;
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
            var user = CurrentUserContext.Current;
            var clinic = MockData.GetClinics().FirstOrDefault(c => c.OwnerUserId == user?.Id);
            ViewBag.Clinic = clinic ?? MockData.GetClinics().FirstOrDefault();
            return View();
        }
    }
}
