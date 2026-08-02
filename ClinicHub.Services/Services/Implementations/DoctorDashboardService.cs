using System.Text;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.Routes.Api;
using ClinicHub.Services.Utilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Services.Implementations
{
    public class DoctorDashboardService : IDoctorDashboardService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public DoctorDashboardService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<DoctorDashboardStatsDto> GetStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.DoctorDashboard.Stats);
                return await _deserializerService.DeserializeApiResponse<DoctorDashboardStatsDto>(response, "حدث خطأ في جلب إحصائيات الطبيب");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<DoctorAppointmentDto>?> GetRecentAppointmentsAsync(int limit = 5)
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.DoctorDashboard.RecentAppointments(limit));
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var obj = JsonConvert.DeserializeObject<JObject>(body);
                var dataToken = obj?["data"] ?? obj?["Data"];
                var dataJson = dataToken?.ToString() ?? body;
                return JsonConvert.DeserializeObject<List<DoctorAppointmentDto>>(dataJson, _jsonSettings) ?? new List<DoctorAppointmentDto>();
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<PagginatedResult<DoctorAppointmentDto>> GetAppointmentsAsync(int? status = null, string? searchTerm = null, string? startDate = null, string? endDate = null, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var url = $"{DoctoryRoutes.DoctorDashboard.Appointments}?pageNumber={pageNumber}&pageSize={pageSize}";
                if (status.HasValue) url += $"&status={status.Value}";
                if (!string.IsNullOrWhiteSpace(searchTerm)) url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
                if (!string.IsNullOrWhiteSpace(startDate)) url += $"&startDate={Uri.EscapeDataString(startDate)}";
                if (!string.IsNullOrWhiteSpace(endDate)) url += $"&endDate={Uri.EscapeDataString(endDate)}";

                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var obj = JsonConvert.DeserializeObject<JObject>(body);
                var dataToken = obj?["data"] ?? obj?["Data"];
                var dataJson = dataToken?.ToString() ?? body;
                return JsonConvert.DeserializeObject<PagginatedResult<DoctorAppointmentDto>>(dataJson, _jsonSettings) ?? new PagginatedResult<DoctorAppointmentDto>(new List<DoctorAppointmentDto>(), 0);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<AppointmentAcceptResponseDto?> UpdateStatusAsync(Guid id, int status, string? notes = null)
        {
            try
            {
                var payload = new { status, notes };
                var json = JsonConvert.SerializeObject(payload, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(DoctoryRoutes.DoctorDashboard.Status(id), content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                return AcceptResponseParser.Parse(body);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<AppointmentAcceptResponseDto?> AcceptAppointmentAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.PutAsync(DoctoryRoutes.DoctorDashboard.AcceptAppointment(id), null);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                return AcceptResponseParser.Parse(body);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> RejectAppointmentAsync(Guid id, string? reason = null)
        {
            try
            {
                var payload = reason != null ? new { reason } : null;
                var json = payload != null ? JsonConvert.SerializeObject(payload, _jsonSettings) : "";
                var content = payload != null ? new StringContent(json, Encoding.UTF8, "application/json") : null;
                var response = await _httpClient.PutAsync(DoctoryRoutes.DoctorDashboard.RejectAppointment(id), content);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في رفض الموعد");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> CompleteAppointmentAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.PutAsync(DoctoryRoutes.DoctorDashboard.CompleteAppointment(id), null);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في إكمال الموعد");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<PagginatedResult<DoctorPatientDto>> GetPatientsAsync(string? searchTerm = null, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var url = $"{DoctoryRoutes.DoctorDashboard.Patients}?pageNumber={pageNumber}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(searchTerm)) url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";

                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var obj = JsonConvert.DeserializeObject<JObject>(body);
                var dataToken = obj?["data"] ?? obj?["Data"];
                var dataJson = dataToken?.ToString() ?? body;
                return JsonConvert.DeserializeObject<PagginatedResult<DoctorPatientDto>>(dataJson, _jsonSettings) ?? new PagginatedResult<DoctorPatientDto>(new List<DoctorPatientDto>(), 0);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<PagginatedResult<PatientHistoryDto>> GetPatientHistoryAsync(Guid patientId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var url = $"{DoctoryRoutes.DoctorDashboard.PatientHistory(patientId)}?pageNumber={pageNumber}&pageSize={pageSize}";
                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var obj = JsonConvert.DeserializeObject<JObject>(body);
                var dataToken = obj?["data"] ?? obj?["Data"];
                var dataJson = dataToken?.ToString() ?? body;
                return JsonConvert.DeserializeObject<PagginatedResult<PatientHistoryDto>>(dataJson, _jsonSettings) ?? new PagginatedResult<PatientHistoryDto>(new List<PatientHistoryDto>(), 0);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
