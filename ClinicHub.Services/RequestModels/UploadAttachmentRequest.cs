using ClinicHub.Services.Enums;
using Microsoft.AspNetCore.Http;

namespace ClinicHub.Services.RequestModels
{
    public record UploadAttachmentRequest(IFormFile File, int Place, MediaType FileType);
}
