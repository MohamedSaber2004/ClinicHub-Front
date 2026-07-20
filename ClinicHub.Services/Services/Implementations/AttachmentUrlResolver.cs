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

            if (fileName.StartsWith("http://") || fileName.StartsWith("https://"))
                return fileName;

            var clean = fileName.TrimStart('/');
            if (clean.StartsWith("files/", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring("files/".Length);

            return $"{_baseUrl}/files/{clean}";
        }
    }
}
