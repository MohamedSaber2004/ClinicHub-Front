using ClinicHub.Data;
using ClinicHub.Models;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ClinicHub.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPlanService _planService;
        private readonly IOptions<GoogleMapsOptions> _googleMapsOptions;

        public HomeController(ILogger<HomeController> logger, IPlanService planService, IOptions<GoogleMapsOptions> googleMapsOptions)
        {
            _logger = logger;
            _planService = planService;
            _googleMapsOptions = googleMapsOptions;
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

        public async Task<IActionResult> Subscriptions()
        {
            try
            {
                ViewBag.Plans = await _planService.GetAllAsync();
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Plans = new List<Services.ReponseModels.PlanDto>();
            }
            return View();
        }

        public async Task<IActionResult> ClinicRegister()
        {
            try
            {
                ViewBag.Plans = await _planService.GetAllAsync();
            }
            catch (ApiException)
            {
                ViewBag.Plans = new List<Services.ReponseModels.PlanDto>();
            }
            ViewBag.GoogleMapsApiKey = _googleMapsOptions.Value.ApiKey;
            return View();
        }

        public IActionResult RegistrationSubmitted()
        {
            return View();
        }

        public IActionResult PendingApproval()
        {
            return View();
        }

        public IActionResult SubscriptionRequired()
        {
            return View();
        }

        public IActionResult PaymentResult(bool success = false)
        {
            ViewBag.PaymentSuccess = success;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("ServiceUnavailable");
        }
    }
}
