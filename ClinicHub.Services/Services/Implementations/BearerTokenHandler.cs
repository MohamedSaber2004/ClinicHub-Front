using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace ClinicHub.Services.Services.Implementations
{
    public class BearerTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly string[] _skipAuthPaths =
        [
            "/auth/login",
            "/auth/refresh-token",
            "/auth/forget-password",
            "/auth/verify-reset-token",
            "/auth/reset-password"
        ];

        public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            if (!_skipAuthPaths.Any(p => path.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                var token = _httpContextAccessor.HttpContext?.Request.Cookies["AccessToken"];
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
