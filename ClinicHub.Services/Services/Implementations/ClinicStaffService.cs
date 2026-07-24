using System.Text;
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
    public class ClinicStaffService : IClinicStaffService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public ClinicStaffService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<PagginatedResult<StaffDto>> GetStaffAsync(int pageNumber = 1, int pageSize = 20, string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                var url = $"{DoctoryRoutes.Staff.List}?pageNumber={pageNumber}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                    url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
                if (isActive.HasValue)
                    url += $"&isActive={isActive.Value.ToString().ToLower()}";

                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    var combined = string.Join(" ", errors);
                    throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب الموظفين" : combined);
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<PagginatedResult<StaffDto>>>(body, _jsonSettings);
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                    return apiResponse.Data;

                var paginated = JsonConvert.DeserializeObject<PagginatedResult<StaffDto>>(body, _jsonSettings);
                return paginated ?? new PagginatedResult<StaffDto>(new List<StaffDto>(), 0, pageNumber, pageSize);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<Guid> CreateStaffAsync(CreateStaffRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.Staff.Create, content);
                var body = await response.Content.ReadAsStringAsync();

                var errors = ApiErrorExtractor.ExtractErrors(body);
                if (!response.IsSuccessStatusCode)
                {
                    var combined = string.Join(" ", errors);
                    throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في إضافة الموظف" : combined);
                }

                if (Guid.TryParse(body.Trim('"'), out var id))
                    return id;

                var obj = JsonConvert.DeserializeObject<JObject>(body);
                var dataToken = obj?["data"] ?? obj?["Data"];
                if (dataToken != null && Guid.TryParse(dataToken.ToString(), out var dataId))
                    return dataId;

                var errorsCombined = string.Join(" ", errors);
                throw new ApiException(400, string.IsNullOrWhiteSpace(errorsCombined) ? "فشل إضافة الموظف" : errorsCombined);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> UpdateStaffAsync(Guid id, UpdateStaffRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(DoctoryRoutes.Staff.Update(id), content);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في تحديث الموظف");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> DeleteStaffAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(DoctoryRoutes.Staff.Delete(id));
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في حذف الموظف");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}