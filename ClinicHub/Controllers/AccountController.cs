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
        public IActionResult ForgotPassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword == confirmPassword)
            {
                // Dummy success redirect
                TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح";
                return RedirectToAction("Login");
            }
            ModelState.AddModelError("", "كلمة المرور الجديدة غير متطابقة");
            return View();
        }
    }
}
