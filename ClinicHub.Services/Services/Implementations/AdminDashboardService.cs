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
    public class AdminDashboardService : IAdminDashboardService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public AdminDashboardService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<AdminDashboardStatsDto> GetStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.AdminDashboard.Stats);
                return await _deserializerService.DeserializeApiResponse<AdminDashboardStatsDto>(response, "حدث خطأ في جلب إحصائيات لوحة التحكم");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<SupportTicketDto>> GetUrgentTicketsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.AdminDashboard.UrgentTickets);
                return await _deserializerService.DeserializeApiResponse<List<SupportTicketDto>>(response, "حدث خطأ في جلب التذاكر العاجلة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<PagginatedResult<SubscriptionDto>> GetSubscriptionsAsync(int pageNumber = 1, int pageSize = 5)
        {
            try
            {
                var url = $"{DoctoryRoutes.AdminDashboard.Subscriptions}?pageNumber={pageNumber}&pageSize={pageSize}";
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
                return JsonConvert.DeserializeObject<PagginatedResult<SubscriptionDto>>(dataJson, _jsonSettings) ?? new PagginatedResult<SubscriptionDto>(new List<SubscriptionDto>(), 0);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<PagginatedResult<SupportTicketDto>> GetTicketsAsync(int? status = null, int? priority = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var url = $"{DoctoryRoutes.AdminDashboard.Tickets}?pageNumber={pageNumber}&pageSize={pageSize}";
                if (status.HasValue) url += $"&status={status.Value}";
                if (priority.HasValue) url += $"&priority={priority.Value}";

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
                return JsonConvert.DeserializeObject<PagginatedResult<SupportTicketDto>>(dataJson, _jsonSettings) ?? new PagginatedResult<SupportTicketDto>(new List<SupportTicketDto>(), 0);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> UpdateTicketStatusAsync(Guid id, int status)
        {
            try
            {
                var json = JsonConvert.SerializeObject(new { status }, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(DoctoryRoutes.AdminDashboard.UpdateTicketStatus(id), content);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في تحديث حالة التذكرة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
