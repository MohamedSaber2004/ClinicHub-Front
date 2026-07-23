using System.Net.Http.Headers;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using ClinicHub.Services.Routes.Api;
using ClinicHub.Services.Utilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Services.Implementations
{
    public class AttachmentService : IAttachmentService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;

        private readonly string _uploadAttachment;
        private readonly string _uploadMultipleAttachments;
        private readonly Func<string, string> _updateAttachment;
        private readonly string _downloadAttachment;

        public AttachmentService(HttpClient httpClient, IOptions<Doctory> doctoryOptions)
        {
            _httpClient = httpClient;

            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _uploadAttachment = DoctoryRoutes.Attachments.Upload;
            _uploadMultipleAttachments = DoctoryRoutes.Attachments.UploadMultiple;
            _updateAttachment = DoctoryRoutes.Attachments.Update;
            _downloadAttachment = DoctoryRoutes.Attachments.Download;
        }

        public async Task<string> UploadAttachmentAsync(UploadAttachmentRequest request)
        {
            using var formData = new MultipartFormDataContent();

            var fileContent = new StreamContent(request.File.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.File.ContentType);
            formData.Add(fileContent, "File", request.File.FileName);
            formData.Add(new StringContent(request.Place.ToString()), "Place");
            formData.Add(new StringContent(((int)request.FileType).ToString()), "FileType");

            var response = await _httpClient.PostAsync(_uploadAttachment, formData);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في رفع الملف" : combined);
            }

            var result = ExtractFileNameFromResponse(responseBody);
            return result;
        }

        public async Task<List<string>> UploadMultipleAttachmentsAsync(UploadMultipleAttachmentsRequest request)
        {
            using var formData = new MultipartFormDataContent();

            if (request.Images != null)
            {
                foreach (var image in request.Images)
                {
                    var fileContent = new StreamContent(image.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                    formData.Add(fileContent, "Images", image.FileName);
                }
            }
            formData.Add(new StringContent(request.ImagesPlace.ToString()), "ImagesPlace");

            if (request.Videos != null)
            {
                foreach (var video in request.Videos)
                {
                    var fileContent = new StreamContent(video.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(video.ContentType);
                    formData.Add(fileContent, "Videos", video.FileName);
                }
            }
            formData.Add(new StringContent(request.VideosPlace.ToString()), "VideosPlace");

            if (request.Audios != null)
            {
                foreach (var audio in request.Audios)
                {
                    var fileContent = new StreamContent(audio.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(audio.ContentType);
                    formData.Add(fileContent, "Audios", audio.FileName);
                }
            }
            formData.Add(new StringContent(request.AudiosPlace.ToString()), "AudiosPlace");

            if (request.Documents != null)
            {
                foreach (var document in request.Documents)
                {
                    var fileContent = new StreamContent(document.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(document.ContentType);
                    formData.Add(fileContent, "Documents", document.FileName);
                }
            }
            formData.Add(new StringContent(request.DocumentsPlace.ToString()), "DocumentsPlace");

            var response = await _httpClient.PostAsync(_uploadMultipleAttachments, formData);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في رفع الملفات" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];

            var dataJson = dataToken?.ToString() ?? responseBody;
            var result = JsonConvert.DeserializeObject<List<string>>(dataJson, _jsonSettings);

            return result!;
        }

        public async Task<string> UpdateAttachmentAsync(UpdateAttachmentRequest request)
        {
            using var formData = new MultipartFormDataContent();

            var fileContent = new StreamContent(request.File.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.File.ContentType);
            formData.Add(fileContent, "File", request.File.FileName);

            if (!string.IsNullOrEmpty(request.OldFileName))
                formData.Add(new StringContent(request.OldFileName), "OldFileName");

            formData.Add(new StringContent(request.Place.ToString()), "Place");
            formData.Add(new StringContent(((int)request.FileType).ToString()), "FileType");

            var url = _updateAttachment(request.OldFileName ?? request.File.FileName);
            var response = await _httpClient.PutAsync(url, formData);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في تحديث الملف" : combined);
            }

            var result = ExtractFileNameFromResponse(responseBody);
            return result;
        }

        private static string ExtractFileNameFromResponse(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody)) return string.Empty;

            try
            {
                var token = JToken.Parse(responseBody);
                if (token is JObject obj)
                {
                    var dataToken = obj["data"] ?? obj["Data"];
                    if (dataToken != null && dataToken.Type != JTokenType.Null)
                    {
                        if (dataToken is JObject dataObj)
                        {
                            var fn = dataObj["filename"] ?? dataObj["fileName"] ?? dataObj["FileName"]
                                  ?? dataObj["url"] ?? dataObj["Url"] ?? dataObj["URL"]
                                  ?? dataObj["path"] ?? dataObj["Path"]
                                  ?? dataObj["file"] ?? dataObj["File"]
                                  ?? dataObj["name"] ?? dataObj["Name"];
                            if (fn != null) return fn.ToString();
                        }
                        else if (dataToken is JValue jVal)
                        {
                            return jVal.ToString() ?? string.Empty;
                        }
                        else
                        {
                            return dataToken.ToString();
                        }
                    }

                    var messageToken = obj["message"] ?? obj["Message"];
                    if (messageToken != null && messageToken.Type != JTokenType.Null)
                    {
                        return messageToken.ToString();
                    }
                }
                else if (token is JValue jVal)
                {
                    return jVal.ToString() ?? string.Empty;
                }
            }
            catch
            {
                if (responseBody.Length < 256) return responseBody;
            }

            return string.Empty;
        }

        public async Task<DownloadAttachmentResponse> DownloadAttachment(DownloadAttachmentRequest request)
        {
            var url = $"{_downloadAttachment}?Place={request.Place}&FileName={Uri.EscapeDataString(request.FileName)}";

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في تحميل الملف" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];

            var dataJson = dataToken?.ToString() ?? responseBody;
            var result = JsonConvert.DeserializeObject<DownloadAttachmentResponse>(dataJson, _jsonSettings);

            return result!;
        }
    }
}
