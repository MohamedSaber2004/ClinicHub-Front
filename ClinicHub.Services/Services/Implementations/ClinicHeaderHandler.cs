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
            var clinicId = _httpContextAccessor.HttpContext?.Items["ClinicId"];
            if (clinicId is Guid id)
            {
                request.Headers.TryAddWithoutValidation("X-ClinicId", id.ToString());
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
