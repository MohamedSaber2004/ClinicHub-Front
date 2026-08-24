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
    public class ClinicDashboardService : IClinicDashboardService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public ClinicDashboardService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<ClinicDashboardStatsDto> GetStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.ClinicDashboard.Stats);
                return await _deserializerService.DeserializeApiResponse<ClinicDashboardStatsDto>(response, "حدث خطأ في جلب إحصائيات العيادة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<ClinicOperationalReportDto> GetOperationalReportAsync(string? period = "week", Guid? doctorId = null)
        {
            try
            {
                var url = DoctoryRoutes.ClinicDashboard.OperationalReport(period, doctorId);
                var response = await _httpClient.GetAsync(url);
                return await _deserializerService.DeserializeApiResponse<ClinicOperationalReportDto>(response, "تعذر تحميل التقرير التشغيلي")
                    ?? new ClinicOperationalReportDto();
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"تعذر تحميل التقرير التشغيلي: {ex.Message}");
            }
        }

        public async Task<List<RevenueTrendPointDto>> GetRevenueTrendAsync(string? granularity = "day", DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var url = DoctoryRoutes.ClinicDashboard.RevenueTrend(granularity ?? "day", fromDate, toDate);
                var response = await _httpClient.GetAsync(url);
                return await _deserializerService.DeserializeApiResponse<List<RevenueTrendPointDto>>(response, "تعذر تحميل رسم الإيرادات") ?? new List<RevenueTrendPointDto>();
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"تعذر تحميل رسم الإيرادات: {ex.Message}");
            }
        }

        public async Task<List<AppointmentsSummaryPointDto>> GetAppointmentsSummaryAsync(string? granularity = "day", DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var url = DoctoryRoutes.ClinicDashboard.AppointmentsSummary(granularity ?? "day", fromDate, toDate);
                var response = await _httpClient.GetAsync(url);
                return await _deserializerService.DeserializeApiResponse<List<AppointmentsSummaryPointDto>>(response, "تعذر تحميل رسم الزيارات") ?? new List<AppointmentsSummaryPointDto>();
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"تعذر تحميل رسم الزيارات: {ex.Message}");
            }
        }

        public async Task<PagginatedResult<ClinicBookingDto>> GetBookingsAsync(string? status = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var url = $"{DoctoryRoutes.ClinicDashboard.Bookings}?pageNumber={pageNumber}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";

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
                return JsonConvert.DeserializeObject<PagginatedResult<ClinicBookingDto>>(dataJson, _jsonSettings) ?? new PagginatedResult<ClinicBookingDto>(new List<ClinicBookingDto>(), 0);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> AcceptBookingAsync(Guid id)
        {
            try
            {
                var json = JsonConvert.SerializeObject(new { bookingId = id }, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.ClinicDashboard.AcceptBooking, content);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في قبول الحجز");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> RejectBookingAsync(Guid id, string? reason = null)
        {
            try
            {
                var payload = new { bookingId = id, reason };
                var json = JsonConvert.SerializeObject(payload, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.ClinicDashboard.RejectBooking, content);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في رفض الحجز");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
