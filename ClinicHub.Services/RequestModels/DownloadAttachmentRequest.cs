namespace ClinicHub.Services.RequestModels
{
    public record DownloadAttachmentRequest(
        int Place, 
        string FileName);
}
