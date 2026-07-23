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
    public class AdminSubscriptionService : IAdminSubscriptionService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly string _listSubscriptionsUrl;
        private readonly string _listPlansUrl;

        public AdminSubscriptionService(HttpClient httpClient, IOptions<Doctory> doctoryOptions)
        {
            _httpClient = httpClient;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _listSubscriptionsUrl = DoctoryRoutes.AdminSubscriptions.ListSubscriptions;
            _listPlansUrl = DoctoryRoutes.AdminSubscriptions.ListPlans;
        }

        public async Task<PagginatedResult<SubscriptionDto>> GetSubscriptionsAsync(GetPaginatedSubscriptionsRequest request)
        {
            var queryParams = new List<string>();
            if (request.Status.HasValue) queryParams.Add($"status={request.Status}");
            if (request.PlanId.HasValue) queryParams.Add($"planId={request.PlanId}");
            if (request.ClinicId.HasValue) queryParams.Add($"clinicId={request.ClinicId}");
            queryParams.Add($"pageNumber={request.PageNumber}");
            queryParams.Add($"pageSize={request.PageSize}");

            var url = _listSubscriptionsUrl + "?" + string.Join("&", queryParams);
            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب الاشتراكات" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];
            var dataJson = dataToken?.ToString() ?? responseBody;
            return JsonConvert.DeserializeObject<PagginatedResult<SubscriptionDto>>(dataJson, _jsonSettings)!;
        }

        public async Task<string> RevokeSubscriptionAsync(RevokeSubscriptionRequest request)
        {
            var url = DoctoryRoutes.AdminSubscriptions.RevokeSubscription(request.SubscriptionId);
            var response = await _httpClient.PostAsync(url, null);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في إلغاء الاشتراك" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"] ?? obj?["message"] ?? obj?["Message"];
            return dataToken?.ToString() ?? "تم إلغاء الاشتراك بنجاح";
        }

        public async Task<List<PlanDto>> GetAllPlansAsync()
        {
            var response = await _httpClient.GetAsync(_listPlansUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب الباقات" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];
            if (dataToken == null) return new();
            return JsonConvert.DeserializeObject<List<PlanDto>>(dataToken.ToString(), _jsonSettings) ?? new();
        }

        public async Task<PlanDto> CreatePlanAsync(PlanDto plan)
        {
            var json = JsonConvert.SerializeObject(plan, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(DoctoryRoutes.AdminSubscriptions.CreatePlan, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في إنشاء الباقة" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];
            var dataJson = dataToken?.ToString() ?? responseBody;
            return JsonConvert.DeserializeObject<PlanDto>(dataJson, _jsonSettings)!;
        }

        public async Task<PlanDto> UpdatePlanAsync(Guid id, PlanDto plan)
        {
            var json = JsonConvert.SerializeObject(plan, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(DoctoryRoutes.AdminSubscriptions.UpdatePlan(id), content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في تحديث الباقة" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];
            var dataJson = dataToken?.ToString() ?? responseBody;
            return JsonConvert.DeserializeObject<PlanDto>(dataJson, _jsonSettings)!;
        }

        public async Task<string> DeletePlanAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync(DoctoryRoutes.AdminSubscriptions.DeletePlan(id));
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في حذف الباقة" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"] ?? obj?["message"] ?? obj?["Message"];
            return dataToken?.ToString() ?? "تم حذف الباقة بنجاح";
        }
    }
}
