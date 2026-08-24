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

        public async Task<AdminUserOverviewDto> GetUserOverviewAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.AdminDashboard.UserOverview(userId));
                return await _deserializerService.DeserializeApiResponse<AdminUserOverviewDto>(response, "تعذر تحميل بيانات المستخدم")
                    ?? new AdminUserOverviewDto();
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"تعذر تحميل بيانات المستخدم: {ex.Message}");
            }
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

        public async Task<List<RevenueTrendPointDto>> GetRevenueTrendAsync(string granularity = "day", DateTime? fromDate = null, DateTime? toDate = null)
            => await GetGraphListAsync<RevenueTrendPointDto>(DoctoryRoutes.AdminDashboard.RevenueTrend(granularity, fromDate, toDate));

        public async Task<List<ClinicsGrowthPointDto>> GetClinicsGrowthAsync(string granularity = "day", DateTime? fromDate = null, DateTime? toDate = null)
            => await GetGraphListAsync<ClinicsGrowthPointDto>(DoctoryRoutes.AdminDashboard.ClinicsGrowth(granularity, fromDate, toDate));

        public async Task<List<SubscriptionsByPlanDto>> GetSubscriptionsByPlanAsync(DateTime? fromDate = null, DateTime? toDate = null)
            => await GetGraphListAsync<SubscriptionsByPlanDto>(DoctoryRoutes.AdminDashboard.SubscriptionsByPlan(fromDate, toDate));

        public async Task<List<UsersGrowthPointDto>> GetUsersGrowthAsync(string granularity = "day", DateTime? fromDate = null, DateTime? toDate = null)
            => await GetGraphListAsync<UsersGrowthPointDto>(DoctoryRoutes.AdminDashboard.UsersGrowth(granularity, fromDate, toDate));

        public async Task<List<AppointmentsSummaryPointDto>> GetAppointmentsSummaryAsync(string granularity = "day", DateTime? fromDate = null, DateTime? toDate = null)
            => await GetGraphListAsync<AppointmentsSummaryPointDto>(DoctoryRoutes.AdminDashboard.AppointmentsSummary(granularity, fromDate, toDate));

        private async Task<List<T>> GetGraphListAsync<T>(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return new List<T>();

                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var obj = JsonConvert.DeserializeObject<JObject>(body);
                var dataToken = obj?["data"] ?? obj?["Data"];
                var dataJson = dataToken?.ToString() ?? body;
                return JsonConvert.DeserializeObject<List<T>>(dataJson, _jsonSettings) ?? new List<T>();
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
