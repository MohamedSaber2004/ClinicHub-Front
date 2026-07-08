using ClinicHub.Services.Contracts;
using ClinicHub.Services.Enums;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

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
                TempData["AccessToken"] = result.AccessToken;
                TempData["RefreshToken"] = result.RefreshToken;

                var redirectUrl = result.Roles.Contains(UserType.SuperAdmin.ToString())
                    ? Url.Action("Index", "Admin")
                    : result.Roles.Contains(UserType.ClinicOwner.ToString())
                        ? Url.Action("Index", "Clinic")
                        : Url.Action("Index", "Admin");

                return RedirectJson(redirectUrl!);
            }
            catch (ApiException ex)
            {
                return Fail(ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return Fail(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
        {
            var token = request?.RefreshToken;

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    await _authService.LogoutAsync(new LogoutRequest(token));
                }
                catch
                {
                    // ignore backend error — always clear local session
                }
            }

            return RedirectJson(Url.Action("Login"));
        }

        [HttpPost]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request);

                return Json(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken
                });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = ex.StatusCode;
                return Json(new { errors = new List<string> { ex.Message } });
            }
            catch
            {
                Response.StatusCode = 500;
                return Json(new { errors = new List<string> { "خطأ في الاتصال بالخادم." } });
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Fail(400, "يرجى إدخال البريد الإلكتروني.");

            try
            {
                await _authService.ForgetPasswordAsync(new ForgetPasswordRequest(email));
                TempData["Email"] = email;
                return RedirectJson(Url.Action("VerifyCode"));
            }
            catch (ApiException ex)
            {
                return Fail(ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return Fail(500, ex.Message);
            }
        }

        [HttpGet]
        public IActionResult VerifyCode()
        {
            if (TempData["Email"] == null)
                return RedirectToAction("ForgotPassword");

            TempData.Keep("Email");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyCode(string verificationCode)
        {
            var email = TempData["Email"]?.ToString();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(verificationCode))
                return RedirectToAction("ForgotPassword");

            try
            {
                var isValid = await _authService.VerifyResetTokenAsync(new VerifyResetTokenRequest(verificationCode, email));

                if (!isValid)
                {
                    TempData.Keep("Email");
                    return Fail(400, "رمز التحقق غير صحيح.");
                }

                TempData["Email"] = email;
                TempData["CodeVerified"] = "true";
                TempData["VerificationCode"] = verificationCode;

                return RedirectJson(Url.Action("ResetPassword"));
            }
            catch (ApiException ex)
            {
                TempData.Keep("Email");
                TempData.Keep("VerificationCode");
                return Fail(ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                TempData.Keep("Email");
                TempData.Keep("VerificationCode");
                return Fail(500, ex.Message);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["Email"] == null || TempData["CodeVerified"]?.ToString() != "true")
                return RedirectToAction("ForgotPassword");

            TempData.Keep("Email");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
        {
            var email = TempData["Email"]?.ToString();
            var token = TempData["VerificationCode"]?.ToString();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToAction("ForgotPassword");

            if (newPassword != confirmPassword)
            {
                TempData.Keep("Email");
                TempData.Keep("VerificationCode");
                return Fail(400, "كلمة المرور الجديدة غير متطابقة.");
            }

            try
            {
                var success = await _authService.ResetPasswordAsync(new ResetPasswordRequest(email, token, newPassword, confirmPassword));
                if (!success)
                {
                    TempData.Keep("Email");
                    TempData.Keep("VerificationCode");
                    return Fail(400, "فشلت عملية إعادة تعيين كلمة المرور. يرجى المحاولة مرة أخرى.");
                }

                TempData["SuccessMessage"] = "تم إعادة تعيين كلمة المرور بنجاح. يمكنك تسجيل الدخول الآن.";
                return RedirectJson(Url.Action("Login"));
            }
            catch (ApiException ex)
            {
                TempData.Keep("Email");
                TempData.Keep("VerificationCode");
                return Fail(ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                TempData.Keep("Email");
                TempData.Keep("VerificationCode");
                return Fail(500, ex.Message);
            }
        }
    }
}
