using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ClinicHub.Services.Services.Implementations
{
    public class BearerTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        // Paths that must NEVER send a token (strictly public/anonymous, e.g. auth flows)
        private static readonly string[] _neverSendTokenPaths =
        [
            "/auth/login",
            "/auth/refresh-token",
            "/auth/forget-password",
            "/auth/verify-reset-token",
            "/auth/reset-password",
            "/specializations/active",  // anonymous public endpoint for active specializations
            "/clinics/register"
        ];

        // Paths that are anonymous-friendly — send token only if one exists in the cookie.
        // These endpoints work with or without authentication (e.g. public listing endpoints).
        private static readonly string[] _publicEndpoints =
        [
            "/api/v1/specializations",
            "/api/v1/plans",
            "/api/v1/clinics/register"
        ];

        // Serializes concurrent refresh attempts across parallel requests so only
        // one refresh call hits the backend at a time.
        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        public BearerTokenHandler(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
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

            var token = ReadAccessToken();

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

            var response = await base.SendAsync(request, cancellationToken);

            // 2. Root-cause self-healing: on 401/403 the JWT is expired OR its claims are
            //    stale (role approved after login, clinic linked later, etc.). Refresh once,
            //    persist the new cookies, and transparently retry the original request.
            bool isAuthError = (int)response.StatusCode == StatusCodes.Status401Unauthorized
                            || (int)response.StatusCode == StatusCodes.Status403Forbidden;
            bool canRefresh = !isNeverAuth && !string.IsNullOrEmpty(token);

            if (isAuthError && canRefresh)
            {
                var refreshed = await TryRefreshTokenAsync(token, request.RequestUri!, cancellationToken);
                if (!string.IsNullOrEmpty(refreshed))
                {
                    var retryRequest = await CloneRequestAsync(request, cancellationToken);
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed);
                    return await base.SendAsync(retryRequest, cancellationToken);
                }
            }

            return response;
        }

        private string? ReadAccessToken()
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies["AccessToken"]
                ?? _httpContextAccessor.HttpContext?.Request.Cookies["accessToken"];
        }

        /// <summary>
        /// Calls POST /auth/refresh-token using the RefreshToken cookie. Returns the new
        /// access token, or null when refreshing is impossible/failed. Concurrent callers
        /// are deduplicated: if another request already rotated the cookie, the new value
        /// is picked up without hitting the backend again.
        /// </summary>
        private async Task<string?> TryRefreshTokenAsync(string? tokenUsed, Uri requestUri, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var refreshToken = httpContext?.Request.Cookies["RefreshToken"]
                            ?? httpContext?.Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                return null;

            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                // Another parallel request may have refreshed while we waited.
                var currentCookie = ReadAccessToken();
                if (!string.IsNullOrEmpty(currentCookie) &&
                    !string.Equals(currentCookie, tokenUsed, StringComparison.Ordinal))
                {
                    return currentCookie;
                }

                var baseUrl = requestUri.GetLeftPart(UriPartial.Authority);
                var cleanClient = _httpClientFactory.CreateClient("BearerTokenRefresh");

                var payload = JsonSerializer.Serialize(new { refreshToken });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");

                using var refreshResponse = await cleanClient.PostAsync($"{baseUrl}/api/{Routes.Api.DoctoryRoutes.Version}/auth/refresh-token", content, cancellationToken);
                if (!refreshResponse.IsSuccessStatusCode)
                    return null;

                var body = await refreshResponse.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(body);

                var data = doc.RootElement.TryGetProperty("data", out var dataEl) ? dataEl : default;
                if (data.ValueKind != JsonValueKind.Object ||
                    !data.TryGetProperty("accessToken", out var accessEl) ||
                    !data.TryGetProperty("refreshToken", out var refreshEl))
                    return null;

                var newAccess = accessEl.GetString();
                var newRefresh = refreshEl.GetString();

                if (string.IsNullOrEmpty(newAccess))
                    return null;

                PersistTokens(newAccess, newRefresh);
                return newAccess;
            }
            catch
            {
                return null;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private void PersistTokens(string accessToken, string? refreshToken)
        {
            try
            {
                var response = _httpContextAccessor.HttpContext?.Response;
                if (response?.HasStarted ?? true)
                    return;

                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                };

                response.Cookies.Append("AccessToken", accessToken, options);
                if (!string.IsNullOrEmpty(refreshToken))
                    response.Cookies.Append("RefreshToken", refreshToken!, options);
            }
            catch
            {
                // Best-effort persistence — the retried request still succeeds with the
                // in-memory token even if cookie writing fails.
            }
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version
            };

            if (request.Content != null)
            {
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;
                var clonedContent = new StreamContent(ms);
                foreach (var header in request.Content.Headers)
                    clonedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                clone.Content = clonedContent;
            }

            foreach (var header in request.Headers)
            {
                if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                    continue;
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
