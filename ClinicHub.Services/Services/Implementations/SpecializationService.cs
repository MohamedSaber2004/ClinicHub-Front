using System.Text;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Enums;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using ClinicHub.Services.Routes.Api;
using ClinicHub.Services.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Services.Implementations
{
    public class SpecializationService: ISpecializationService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IAttachmentService _attachmentService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDeserializerService _deserializerService;

        private readonly string _getAllSpecializations;
        private readonly Func<Guid, string> _getSpecializationById;
        private readonly string _createSpecialization;
        private readonly string _updateSpecialization;
        private readonly string _deleteSpecialization;

        public SpecializationService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IAttachmentService attachmentService, IHttpContextAccessor httpContextAccessor, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _attachmentService = attachmentService;
            _httpContextAccessor = httpContextAccessor;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _getAllSpecializations = DoctoryRoutes.Specializations.GetAll;
            _getSpecializationById = DoctoryRoutes.Specializations.GetById;
            _createSpecialization = DoctoryRoutes.Specializations.Create;
            _updateSpecialization = DoctoryRoutes.Specializations.Update;
            _deleteSpecialization = DoctoryRoutes.Specializations.Delete;
        }

        public async Task<PagginatedResult<SpecializationDto>> GetAllAsync(int pageNumber = 1, int pageSize = 20, bool? isFamous = null)
        {
            try
            {
                var url = $"{_getAllSpecializations}?PageNumber={pageNumber}&PageSize={pageSize}";
                if (isFamous.HasValue)
                    url += $"&IsFamous={isFamous.Value.ToString().ToLower()}";
                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    var combined = string.Join(" ", errors);
                    throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب التخصصات" : combined);
                }

                var obj = JsonConvert.DeserializeObject<JObject>(body);
                if (obj != null)
                {
                    var dataObj = obj["data"] ?? obj["Data"];
                    var source = dataObj is JObject dObj ? dObj : obj;

                    var itemsToken = source["items"] ?? source["data"] ?? source["records"];
                    var totalCount = source["totalCount"]?.Value<int>()
                        ?? source["total"]?.Value<int>()
                        ?? source["totalRecords"]?.Value<int>()
                        ?? 0;
                    var actualPage = source["pageNumber"]?.Value<int>() ?? pageNumber;
                    var actualPageSize = source["pageSize"]?.Value<int>() ?? pageSize;

                    if (itemsToken != null)
                    {
                        var items = JsonConvert.DeserializeObject<List<SpecializationDto>>(itemsToken.ToString(), _jsonSettings) ?? new();
                        if (totalCount == 0) totalCount = items.Count;
                        return new PagginatedResult<SpecializationDto>(items, totalCount, actualPage, actualPageSize);
                    }
                }

                return new PagginatedResult<SpecializationDto>(new List<SpecializationDto>(), 0, pageNumber, pageSize);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<ApiResponse<SpecializationDto?>> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync(_getSpecializationById(id));
                var body = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<SpecializationDto?>>(body, _jsonSettings);

                if (apiResponse != null && apiResponse.Success)
                    return apiResponse;

                var errors = ApiErrorExtractor.ExtractErrors(body);
                var combined = string.Join(" ", errors);
                if (string.IsNullOrWhiteSpace(combined))
                    combined = apiResponse?.Message ?? "التخصص غير موجود";
                var statusCode = apiResponse?.StatusCode > 0 ? apiResponse.StatusCode : (int)response.StatusCode;
                throw new ApiException(statusCode, combined);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<string> CreateAsync(CreateSpecializationRequest request)
        {
            try
            {
                string? iconUrl = null;
                var iconFile = _httpContextAccessor.HttpContext?.Request.Form.Files.GetFile("Icon");
                if (iconFile != null)
                {
                    var uploadRequest = new UploadAttachmentRequest(iconFile, 13, MediaType.Image);
                    iconUrl = await _attachmentService.UploadAttachmentAsync(uploadRequest);
                }

                var payload = new Dictionary<string, object?>
                {
                    ["Name"] = request.Name,
                    ["ArName"] = request.ArName,
                    ["Description"] = request.Description,
                    ["IsFamous"] = request.IsFamous
                };
                if (!string.IsNullOrEmpty(iconUrl))
                    payload["Icon"] = iconUrl;

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_createSpecialization, content);
                return await _deserializerService.DeserializeApiResponse<string>(response, "حدث خطأ في إضافة التخصص");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<SpecializationDto?> UpdateAsync(UpdateSpecializationRequest request)
        {
            try
            {
                string? iconUrl = request.CurrentIcon;
                var iconFile = _httpContextAccessor.HttpContext?.Request.Form.Files.GetFile("Icon");
                if (iconFile != null)
                {
                    var uploadRequest = new UploadAttachmentRequest(iconFile, 13, MediaType.Image);
                    iconUrl = await _attachmentService.UploadAttachmentAsync(uploadRequest);
                }

                var payload = new Dictionary<string, object?>
                {
                    ["Id"] = request.Id,
                    ["Name"] = request.Name,
                    ["ArName"] = request.ArName,
                    ["Description"] = request.Description,
                    ["IsFamous"] = request.IsFamous
                };
                if (!string.IsNullOrEmpty(iconUrl))
                    payload["Icon"] = iconUrl;

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(_updateSpecialization, content);
                return await _deserializerService.DeserializeApiResponse<SpecializationDto>(response, "حدث خطأ في تحديث التخصص");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<string> DeleteAsync(DeleteSpecializationRequest request)
        {
            try
            {
                var payload = new Dictionary<string, object?> { ["Id"] = request.Id };

                var json = JsonConvert.SerializeObject(payload);
                var req = new HttpRequestMessage(HttpMethod.Delete, _deleteSpecialization)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(req);
                return await _deserializerService.DeserializeApiResponse<string>(response, "حدث خطأ في حذف التخصص");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
