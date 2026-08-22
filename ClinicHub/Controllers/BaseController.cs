using System.Text;
using System.Text.Json;
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

        /// <summary>
        /// Decodes the JWT payload of the AccessToken cookie (no signature verification —
        /// the token's validity is enforced by the backend on every API call) and checks
        /// whether the UserTypes bitmask includes the SuperAdmin bit.
        /// Shared by every gate that must treat admins differently from clinic users.
        /// </summary>
        protected static bool TokenGrantsSuperAdmin(HttpContext httpContext)
        {
            var jwt = httpContext.Request.Cookies["AccessToken"]
                   ?? httpContext.Request.Cookies["accessToken"];
            if (string.IsNullOrWhiteSpace(jwt)) return false;

            var parts = jwt.Split('.');
            if (parts.Length < 2) return false;

            try
            {
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = (payload.Length % 4) switch
                {
                    2 => payload + "==",
                    3 => payload + "=",
                    _ => payload
                };

                using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
                var root = doc.RootElement;

                foreach (var claimName in new[] { "UserTypes", "usertypes", "userTypes" })
                {
                    if (root.TryGetProperty(claimName, out var ut) &&
                        int.TryParse(ut.ToString(), out var mask))
                    {
                        const int SuperAdminBit = 2; // UserType.SuperAdmin
                        return (mask & SuperAdminBit) != 0;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

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

        /// <summary>
        /// Loads the unread notification count (GET /notifications/count) for the layout bell
        /// badge. Fails silently so pages render with a hidden badge when the API is unreachable.
        /// </summary>
        protected async Task LoadNotificationsAsync(INotificationService notificationService)
        {
            try
            {
                ViewBag.UnreadNotificationsCount = await notificationService.GetUnreadCountAsync();
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
