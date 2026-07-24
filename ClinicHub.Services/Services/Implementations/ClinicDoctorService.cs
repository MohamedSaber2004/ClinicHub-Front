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
    public class ClinicDoctorService : IClinicDoctorService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public ClinicDoctorService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<PagginatedResult<DoctorDto>> GetDoctorsAsync(Guid clinicId, int pageNumber = 1, int pageSize = 20, string? searchTerm = null, Guid? specializationId = null)
        {
            try
            {
                var url = $"{DoctoryRoutes.Doctors.ListByClinic(clinicId)}?pageNumber={pageNumber}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                    url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
                if (specializationId.HasValue && specializationId.Value != Guid.Empty)
                    url += $"&specializationId={specializationId}";

                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    var combined = string.Join(" ", errors);
                    throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب الأطباء" : combined);
                }

                var paginated = JsonConvert.DeserializeObject<PagginatedResult<DoctorDto>>(body, _jsonSettings);
                return paginated ?? new PagginatedResult<DoctorDto>(new List<DoctorDto>(), 0, pageNumber, pageSize);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<DoctorDto?> GetDoctorByIdAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.Doctors.GetById(id));
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    var combined = string.Join(" ", errors);
                    throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "الطبيب غير موجود" : combined);
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<DoctorDto?>>(body, _jsonSettings);
                if (apiResponse != null && apiResponse.Success)
                    return apiResponse.Data;

                return JsonConvert.DeserializeObject<DoctorDto>(body, _jsonSettings);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.Doctors.Create, content);
                return await _deserializerService.DeserializeApiResponse<DoctorDto>(response, "حدث خطأ في إضافة الطبيب");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<DoctorDto> UpdateDoctorAsync(Guid id, UpdateDoctorRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(DoctoryRoutes.Doctors.Update(id), content);
                return await _deserializerService.DeserializeApiResponse<DoctorDto>(response, "حدث خطأ في تحديث الطبيب");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> DeleteDoctorAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(DoctoryRoutes.Doctors.Delete(id));
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في حذف الطبيب");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}