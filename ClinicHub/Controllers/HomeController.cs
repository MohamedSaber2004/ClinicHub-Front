using ClinicHub.Data;
using ClinicHub.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ClinicHub.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Subscriptions()
        {
            ViewBag.Plans = MockData.GetActiveSubscriptionPlans();
            return View();
        }

        public IActionResult ClinicRegister()
        {
            ViewBag.Plans = MockData.GetActiveSubscriptionPlans();
            return View();
        }

        public IActionResult RegistrationSubmitted()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("ServiceUnavailable");
        }
    }
}
