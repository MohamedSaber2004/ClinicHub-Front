using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IAttachmentService
    {
        Task<string> UploadAttachmentAsync(UploadAttachmentRequest request);
        Task<List<string>> UploadMultipleAttachmentsAsync(UploadMultipleAttachmentsRequest request);
        Task<string> UpdateAttachmentAsync(UpdateAttachmentRequest request);
        Task<DownloadAttachmentResponse> DownloadAttachment(DownloadAttachmentRequest request);
    }
}
