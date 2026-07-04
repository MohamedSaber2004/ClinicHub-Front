using ClinicHub.Services.Contracts;
using ClinicHub.Services.Enums;
using ClinicHub.Services.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        private bool IsAjaxRequest => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var result = await _authService.LoginAsync(new LoginRequest(email, password));

                TempData["UserId"] = result.Id.ToString();
                TempData["Role"] = result.Roles;
                TempData["ClinicId"] = result.ClinicId?.ToString();

                var redirectUrl = result.Roles.Contains(UserType.SuperAdmin.ToString())
                    ? Url.Action("Index", "Admin")
                    : result.Roles.Contains(UserType.ClinicOwner.ToString())
                        ? Url.Action("Index", "Clinic")
                        : Url.Action("Index", "Admin");

                if (IsAjaxRequest)
                    return Json(new { redirectUrl });

                return Redirect(redirectUrl!);
            }
            catch (Exception ex)
            {
                var errors = new List<string> { ex.Message };

                if (IsAjaxRequest)
                {
                    Response.StatusCode = 400;
                    return Json(new { errors });
                }

                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                if (IsAjaxRequest)
                {
                    Response.StatusCode = 400;
                    return Json(new { errors = new List<string> { "يرجى إدخال البريد الإلكتروني." } });
                }
                ModelState.AddModelError("", "يرجى إدخال البريد الإلكتروني.");
                return View();
            }

            // Generate a random 6-digit verification code
            string randomCode = new System.Random().Next(100000, 999999).ToString();
            
            TempData["Email"] = email;
            TempData["VerificationCode"] = randomCode;

            if (IsAjaxRequest)
                return Json(new { redirectUrl = Url.Action("VerifyCode") });

            return RedirectToAction("VerifyCode");
        }

        [HttpGet]
        public IActionResult VerifyCode()
        {
            if (TempData["Email"] == null || TempData["VerificationCode"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }
            
            TempData.Keep("Email");
            TempData.Keep("VerificationCode");
            return View();
        }

        [HttpPost]
        public IActionResult VerifyCode(string verificationCode)
        {
            var email = TempData["Email"]?.ToString();
            var expectedCode = TempData["VerificationCode"]?.ToString();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(expectedCode))
            {
                if (IsAjaxRequest)
                    return Json(new { redirectUrl = Url.Action("ForgotPassword") });

                ModelState.AddModelError("", "انتهت صلاحية الجلسة، يرجى المحاولة مرة أخرى.");
                return RedirectToAction("ForgotPassword");
            }

            if (verificationCode == expectedCode)
            {
                TempData["Email"] = email;
                TempData["CodeVerified"] = "true";

                if (IsAjaxRequest)
                    return Json(new { redirectUrl = Url.Action("ResetPassword") });

                return RedirectToAction("ResetPassword");
            }

            if (IsAjaxRequest)
            {
                Response.StatusCode = 400;
                return Json(new { errors = new List<string> { "رمز التحقق غير صحيح، يرجى المحاولة مرة أخرى." } });
            }

            ModelState.AddModelError("", "رمز التحقق غير صحيح، يرجى المحاولة مرة أخرى.");
            TempData.Keep("Email");
            TempData.Keep("VerificationCode");
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["Email"] == null || TempData["CodeVerified"]?.ToString() != "true")
            {
                return RedirectToAction("ForgotPassword");
            }

            TempData.Keep("Email");
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string newPassword, string confirmPassword)
        {
            var email = TempData["Email"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                if (IsAjaxRequest)
                    return Json(new { redirectUrl = Url.Action("ForgotPassword") });

                return RedirectToAction("ForgotPassword");
            }

            if (newPassword != confirmPassword)
            {
                if (IsAjaxRequest)
                {
                    Response.StatusCode = 400;
                    return Json(new { errors = new List<string> { "كلمة المرور الجديدة غير متطابقة." } });
                }

                ModelState.AddModelError("", "كلمة المرور الجديدة غير متطابقة.");
                TempData.Keep("Email");
                return View();
            }

            // Simulated success
            TempData["SuccessMessage"] = "تم إعادة تعيين كلمة المرور بنجاح. يمكنك تسجيل الدخول الآن.";

            if (IsAjaxRequest)
                return Json(new { redirectUrl = Url.Action("Login") });

            return RedirectToAction("Login");
        }
    }
}
