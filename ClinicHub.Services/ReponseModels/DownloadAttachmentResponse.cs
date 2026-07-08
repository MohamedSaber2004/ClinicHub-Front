using Microsoft.VisualBasic;

namespace ClinicHub.Services.ReponseModels
{
    public record DownloadAttachmentResponse(
        string FilePath,
        string FileName,
        string ContentType,
        bool Success,
        string? ErrorMessage);
}
