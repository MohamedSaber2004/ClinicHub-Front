using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string clinicCode, string email, string password)
        {
            // Dummy authentication for demo purposes
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                // Redirect to Admin dashboard
                return RedirectToAction("Index", "Admin");
            }
            ModelState.AddModelError("", "خطأ في البريد الإلكتروني أو كلمة المرور");
            return View();
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
                ModelState.AddModelError("", "يرجى إدخال البريد الإلكتروني.");
                return View();
            }

            // Generate a random 6-digit verification code
            string randomCode = new System.Random().Next(100000, 999999).ToString();
            
            TempData["Email"] = email;
            TempData["VerificationCode"] = randomCode;
            
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
                ModelState.AddModelError("", "انتهت صلاحية الجلسة، يرجى المحاولة مرة أخرى.");
                return RedirectToAction("ForgotPassword");
            }

            if (verificationCode == expectedCode)
            {
                TempData["Email"] = email;
                TempData["CodeVerified"] = "true";
                return RedirectToAction("ResetPassword");
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
                return RedirectToAction("ForgotPassword");
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "كلمة المرور الجديدة غير متطابقة.");
                TempData.Keep("Email");
                return View();
            }

            // Simulated success
            TempData["SuccessMessage"] = "تم إعادة تعيين كلمة المرور بنجاح. يمكنك تسجيل الدخول الآن.";
            return RedirectToAction("Login");
        }
    }
}
