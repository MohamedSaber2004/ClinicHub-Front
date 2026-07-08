using ClinicHub.Services.Enums;
using Microsoft.AspNetCore.Http;

namespace ClinicHub.Services.RequestModels
{
    public record UpdateAttachmentRequest(
        IFormFile File,
        string? OldFileName,
        int Place,
        MediaType FileType);
}
