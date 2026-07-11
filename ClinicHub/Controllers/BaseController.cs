using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;

namespace ClinicHub.Controllers
{
    public abstract class BaseController : Controller
    {
        protected bool IsAjaxRequest => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        protected IActionResult RedirectJson(string? redirectUrl)
        {
            if (IsAjaxRequest)
                return Json(new { redirectUrl });
            return Redirect(redirectUrl ?? "/");
        }

        protected IActionResult Fail(int statusCode, string message)
        {
            if (IsAjaxRequest)
        {
                Response.StatusCode = statusCode;
                return Json(new { errors = new List<string> { message } });
            }
            ModelState.AddModelError("", message);
            return View();
        }
    }
}
