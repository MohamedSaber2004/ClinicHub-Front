using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
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

        private IMemoryCache Cache => HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

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
        /// Cached 60s per access token so navigating between pages doesn't re-hit the
        /// API on every request. Fails silently so pages render with fallback
        /// placeholders when the API is unreachable.
        /// </summary>
        protected async Task LoadHeaderProfileAsync(IAuthService authService)
        {
            var cacheKey = $"hdr:profile:{TokenFingerprint()}";
            if (Cache.TryGetValue(cacheKey, out UserProfileDto? cached) && cached is not null)
            {
                ViewBag.HeaderProfile = cached;
                return;
            }

            try
            {
                var profile = await authService.GetProfileAsync();
                ViewBag.HeaderProfile = profile;
                Cache.Set(cacheKey, profile, TimeSpan.FromSeconds(60));
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
        /// badge. Cached 30s per access token. Fails silently so pages render with a hidden
        /// badge when the API is unreachable.
        /// </summary>
        protected async Task LoadNotificationsAsync(INotificationService notificationService)
        {
            var cacheKey = $"notif:count:{TokenFingerprint()}";
            if (Cache.TryGetValue(cacheKey, out int cachedCount))
            {
                ViewBag.UnreadNotificationsCount = cachedCount;
                return;
            }

            try
            {
                var count = await notificationService.GetUnreadCountAsync();
                ViewBag.UnreadNotificationsCount = count;
                Cache.Set(cacheKey, count, TimeSpan.FromSeconds(30));
            }
            catch (ApiException)
            {
            }
            catch (Exception)
            {
            }
        }

        private string TokenFingerprint()
        {
            var token = Request.Cookies["AccessToken"] ?? Request.Cookies["accessToken"] ?? string.Empty;
            var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes, 0, 8);
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
