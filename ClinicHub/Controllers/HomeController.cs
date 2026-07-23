using ClinicHub.Data;
using ClinicHub.Models;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.RequestModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ClinicHub.Controllers
{
public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IPlanService _planService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISpecializationService _specializationService;
    private readonly IAttachmentService _attachmentService;
    private readonly IOptions<GoogleMapsOptions> _googleMapsOptions;

    public HomeController(ILogger<HomeController> logger, IPlanService planService, ISubscriptionService subscriptionService, ISpecializationService specializationService, IAttachmentService attachmentService, IOptions<GoogleMapsOptions> googleMapsOptions)
    {
        _logger = logger;
        _planService = planService;
        _subscriptionService = subscriptionService;
        _specializationService = specializationService;
        _attachmentService = attachmentService;
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

            try
            {
                ViewBag.Specializations = await _specializationService.GetActiveAsync(false);
            }
            catch (ApiException)
            {
                ViewBag.Specializations = new List<Services.ReponseModels.SpecializationDto>();
            }

            ViewBag.GoogleMapsApiKey = _googleMapsOptions.Value.ApiKey;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ClinicRegister(RegisterClinicRequest request)
        {
            try
            {
                var result = await _subscriptionService.RegisterClinicAsync(request);

                if (result.IsPendingApproval)
                {
                    TempData["SuccessMessage"] = "تم تقديم طلب تسجيل العيادة بنجاح! طلبك قيد المراجعة والاعتماد.";
                    return RedirectToAction("PendingApproval");
                }

                TempData["SuccessMessage"] = "تم إنشاء حساب العيادة بنجاح! يمكنك الآن تسجيل الدخول.";
                return RedirectToAction("Login", "Account");
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "عذراً، حدث خطأ أثناء التسجيل. يرجى المحاولة لاحقاً.";
                _logger.LogError(ex, "Clinic registration failed");
            }

            ViewBag.Plans = await _planService.GetAllAsync();
            ViewBag.GoogleMapsApiKey = _googleMapsOptions.Value.ApiKey;
            try
            {
                ViewBag.Specializations = await _specializationService.GetActiveAsync(false);
            }
            catch
            {
                ViewBag.Specializations = new List<Services.ReponseModels.SpecializationDto>();
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttachment(IFormFile file, int place = 5)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Json(new { success = false, error = "الملف مطلوب" });

                var isImage = file.ContentType != null && file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
                var mediaType = isImage ? Services.Enums.MediaType.Image : Services.Enums.MediaType.File;

                var uploadRequest = new UploadAttachmentRequest(file, place, mediaType);
                var fileName = await _attachmentService.UploadAttachmentAsync(uploadRequest);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return Json(new { success = false, error = "فشل رفع الملف أو لم يتم استرجاع اسم الملف بنجاح" });
                }
                return Json(new { success = true, fileName, url = fileName });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload attachment");
                return Json(new { success = false, error = "حدث خطأ أثناء رفع الملف: " + ex.Message });
            }
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
