using ClinicHub.Services.Contracts;
using ClinicHub.Services.Options;
using Microsoft.Extensions.Options;

namespace ClinicHub.Services.Services.Implementations
{
    public class AttachmentUrlResolver : IAttachmentUrlResolver
    {
        private readonly string _baseUrl;

        public AttachmentUrlResolver(IOptions<Doctory> doctoryOptions)
        {
            _baseUrl = doctoryOptions.Value.BaseUrl.TrimEnd('/');
        }

        public string Resolve(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            return $"{_baseUrl}/files/{fileName}";
        }
    }
}
