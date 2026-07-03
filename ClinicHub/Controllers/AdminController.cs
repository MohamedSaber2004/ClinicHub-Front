using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Specializations()
        {
            return View();
        }

        public IActionResult Clinics()
        {
            return View();
        }

        public IActionResult Doctors()
        {
            return View();
        }

        public IActionResult Support()
        {
            return View();
        }

        public IActionResult Ads()
        {
            return View();
        }

        public IActionResult Payments()
        {
            return View();
        }

        public IActionResult PaymentsDetails(int id)
        {
            ViewBag.PaymentId = id;
            return View("PaymentsDetails");
        }

        public IActionResult Profile()
        {
            return View();
        }
    }
}
