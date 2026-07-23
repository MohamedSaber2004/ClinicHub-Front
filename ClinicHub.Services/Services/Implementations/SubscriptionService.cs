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
    public class SubscriptionService : ISubscriptionService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly string _myUrl;
        private readonly string _initiateUrl;
        private readonly string _cancelUrl;
        private readonly string _registerUrl;

        public SubscriptionService(HttpClient httpClient, IOptions<Doctory> doctoryOptions)
        {
            _httpClient = httpClient;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _myUrl = DoctoryRoutes.Subscriptions.My;
            _initiateUrl = DoctoryRoutes.Subscriptions.InitiatePayment;
            _cancelUrl = DoctoryRoutes.Subscriptions.Cancel;
            _registerUrl = DoctoryRoutes.Clinics.Register;
        }

        public async Task<InitiatePaymentResponseDto> InitiatePaymentAsync(InitiatePaymentRequest request)
        {
            var json = JsonConvert.SerializeObject(request, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_initiateUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في بدء الدفع" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];
            var dataJson = dataToken?.ToString() ?? responseBody;
            return JsonConvert.DeserializeObject<InitiatePaymentResponseDto>(dataJson, _jsonSettings)!;
        }

        public async Task<SubscriptionDto> GetMySubscriptionAsync()
        {
            var response = await _httpClient.GetAsync(_myUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب الاشتراك" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];
            var dataJson = dataToken?.ToString() ?? responseBody;
            return JsonConvert.DeserializeObject<SubscriptionDto>(dataJson, _jsonSettings)!;
        }

        public async Task<string> CancelMySubscriptionAsync()
        {
            var response = await _httpClient.PostAsync(_cancelUrl, null);
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

        public async Task<RegisterClinicResponseDto> RegisterClinicAsync(RegisterClinicRequest request)
        {
            var json = JsonConvert.SerializeObject(request, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_registerUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في تسجيل العيادة" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];
            var dataJson = dataToken?.ToString() ?? responseBody;
            var dto = JsonConvert.DeserializeObject<RegisterClinicResponseDto>(dataJson, _jsonSettings) ?? new RegisterClinicResponseDto();

            if (dto.PendingData != null)
            {
                dto.UserId ??= dto.PendingData.UserId;
                dto.Message ??= dto.PendingData.Message;
                dto.IsPendingApproval = dto.PendingData.IsPendingApproval;
            }
            else if (dto.AuthData != null)
            {
                dto.IsPendingApproval = false;
                dto.UserId ??= dto.AuthData.Id;
            }

            return dto;
        }
    }
}
