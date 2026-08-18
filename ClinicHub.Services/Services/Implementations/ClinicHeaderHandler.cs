using Microsoft.AspNetCore.Http;

namespace ClinicHub.Services.Services.Implementations
{
    public class ClinicHeaderHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClinicHeaderHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains("X-ClinicId"))
            {
                var clinicId = _httpContextAccessor.HttpContext?.Items["ClinicId"];
                if (clinicId is Guid id && id != Guid.Empty)
                {
                    request.Headers.TryAddWithoutValidation("X-ClinicId", id.ToString());
                }
                else if (clinicId is string s && Guid.TryParse(s, out var parsedGuid) && parsedGuid != Guid.Empty)
                {
                    request.Headers.TryAddWithoutValidation("X-ClinicId", parsedGuid.ToString());
                }
                else
                {
                    var cookieClinicId = _httpContextAccessor.HttpContext?.Request.Cookies["ClinicId"]
                                      ?? _httpContextAccessor.HttpContext?.Request.Cookies["clinicId"];
                    if (Guid.TryParse(cookieClinicId, out var parsedId) && parsedId != Guid.Empty)
                    {
                        request.Headers.TryAddWithoutValidation("X-ClinicId", parsedId.ToString());
                    }
                    else
                    {
                        var headerClinicId = _httpContextAccessor.HttpContext?.Request.Headers["X-ClinicId"].ToString();
                        if (Guid.TryParse(headerClinicId, out var headerGuid) && headerGuid != Guid.Empty)
                        {
                            request.Headers.TryAddWithoutValidation("X-ClinicId", headerGuid.ToString());
                        }
                    }
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
