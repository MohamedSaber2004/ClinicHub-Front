using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace ClinicHub.Services.Services.Implementations
{
    public class BearerTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Paths that must NEVER send a token (strictly public/anonymous, e.g. auth flows)
        private static readonly string[] _neverSendTokenPaths =
        [
            "/auth/login",
            "/auth/refresh-token",
            "/auth/forget-password",
            "/auth/verify-reset-token",
            "/auth/reset-password",
            "/specializations/active",  // anonymous public endpoint for active specializations
            "/clinics/register",
            "/attachments/upload"
        ];

        // Paths that are anonymous-friendly — send token only if one exists in the cookie.
        // These endpoints work with or without authentication (e.g. public listing endpoints).
        private static readonly string[] _publicEndpoints =
        [
            "/api/v1/specializations",
            "/api/v1/plans",
            "/api/v1/clinics/register"
        ];

        public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            // 1. Never send a token for strictly anonymous auth-flow endpoints
            bool isNeverAuth = _neverSendTokenPaths.Any(p => path.Contains(p, StringComparison.OrdinalIgnoreCase));
            if (isNeverAuth)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var token = _httpContextAccessor.HttpContext?.Request.Cookies["AccessToken"]
                     ?? _httpContextAccessor.HttpContext?.Request.Cookies["accessToken"];

            if (string.IsNullOrEmpty(token))
            {
                var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authHeader.Substring(7).Trim();
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
