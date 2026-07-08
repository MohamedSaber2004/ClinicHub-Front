using Microsoft.AspNetCore.Http;

namespace ClinicHub.Services.RequestModels
{
    public record UploadMultipleAttachmentsRequest(
        List<IFormFile>? Images,
        int ImagesPlace,
        List<IFormFile>? Videos,
        int VideosPlace,
        List<IFormFile>? Audios,
        int AudiosPlace,
        List<IFormFile>? Documents,
        int DocumentsPlace);
}
