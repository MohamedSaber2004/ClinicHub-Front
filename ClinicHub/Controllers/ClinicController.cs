using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.Controllers
{
    public class ClinicController : BaseController
    {
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
    }
}
