using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Controllers
{
    public abstract class BaseController : Controller
    {
        protected CurrentUserContext? CurrentUser { get; set; }
        protected bool IsAjaxRequest => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.CurrentUser = CurrentUser;
            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Loads the logged-in user profile (GET /auth/profile) for the layout header.
        /// Fails silently so pages render with fallback placeholders when the API is unreachable.
        /// </summary>
        protected async Task LoadHeaderProfileAsync(IAuthService authService)
        {
            try
            {
                ViewBag.HeaderProfile = await authService.GetProfileAsync();
            }
            catch (ApiException)
            {
            }
            catch (Exception)
            {
            }
        }

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
