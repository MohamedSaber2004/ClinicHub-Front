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
            catch (Exception ex)
            {
                // Transient failures against the remote API must degrade gracefully,
                // not blow up into the global "service unavailable" page.
                _logger.LogError(ex, "Failed to load plans on Subscriptions page");
                ViewBag.ErrorMessage = "تعذر تحميل الباقات حالياً، يرجى المحاولة بعد قليل.";
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
            catch (Exception)
            {
                ViewBag.Plans = new List<Services.ReponseModels.PlanDto>();
            }

            try
            {
                ViewBag.Specializations = await _specializationService.GetActiveAsync();
            }
            catch (Exception)
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

            try
            {
                ViewBag.Plans = await _planService.GetAllAsync();
            }
            catch (Exception)
            {
                ViewBag.Plans = new List<Services.ReponseModels.PlanDto>();
            }
            ViewBag.GoogleMapsApiKey = _googleMapsOptions.Value.ApiKey;
            try
            {
                ViewBag.Specializations = await _specializationService.GetActiveAsync();
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

        [HttpPost]
        public async Task<IActionResult> UploadDoctorImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "الملف مطلوب" });

                var uploadRequest = new UploadAttachmentRequest(file, 5, Services.Enums.MediaType.Image);
                var fileName = await _attachmentService.UploadAttachmentAsync(uploadRequest);
                if (string.IsNullOrWhiteSpace(fileName))
                    return Json(new { success = false, message = "فشل رفع الصورة" });

                return Json(new { success = true, message = fileName });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload doctor image");
                return Json(new { success = false, message = "حدث خطأ أثناء رفع الصورة: " + ex.Message });
            }
        }

        public async Task<IActionResult> UploadStaffImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "الملف مطلوب" });

                var uploadRequest = new UploadAttachmentRequest(file, 1, Services.Enums.MediaType.Image);
                var fileName = await _attachmentService.UploadAttachmentAsync(uploadRequest);
                if (string.IsNullOrWhiteSpace(fileName))
                    return Json(new { success = false, message = "فشل رفع الصورة" });

                return Json(new { success = true, message = fileName });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload staff image");
                return Json(new { success = false, message = "حدث خطأ أثناء رفع الصورة: " + ex.Message });
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

        public async Task<IActionResult> PaymentResult(bool success = false, string? type = null)
        {
            ViewBag.PaymentSuccess = success;
            ViewBag.PaymentType = type;
            ViewBag.VerificationStatus = null;
            ViewBag.SubscriptionActive = false;
            ViewBag.SubscriptionEndDate = null;

            if (Request.Cookies.ContainsKey("AccessToken"))
            {
                try
                {
                    var verification = await _subscriptionService.VerifyLatestSubscriptionPaymentAsync();
                    ViewBag.VerificationStatus = verification.Status;
                    ViewBag.SubscriptionActive = verification.SubscriptionActive;
                    ViewBag.SubscriptionEndDate = verification.EndDate;
                }
                catch
                {
                    // Verification is best-effort — fall back to the generic result UI.
                }
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            if (feature?.Error != null)
            {
                _logger.LogError(feature.Error, "Unhandled exception at {Path}", feature.Path);
            }
            return View("ServiceUnavailable");
        }
    }
}
