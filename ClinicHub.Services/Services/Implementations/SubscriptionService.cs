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
        private readonly string _cancellationQuoteUrl;
        private readonly string _registerUrl;
        private readonly string _verifyLatestPaymentUrl;

        public SubscriptionService(HttpClient httpClient, IOptions<Doctory> doctoryOptions)
        {
            _httpClient = httpClient;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _myUrl = DoctoryRoutes.Subscriptions.My;
            _initiateUrl = DoctoryRoutes.Subscriptions.InitiatePayment;
            _cancelUrl = DoctoryRoutes.Subscriptions.Cancel;
            _cancellationQuoteUrl = DoctoryRoutes.Subscriptions.CancellationQuote;
            _registerUrl = DoctoryRoutes.Clinics.Register;
            _verifyLatestPaymentUrl = DoctoryRoutes.Subscriptions.VerifyLatestSubscriptionPayment;
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

            if (string.IsNullOrWhiteSpace(responseBody))
                throw new ApiException(500, "استجابة فارغة من الخادم");

            return DeserializeData<InitiatePaymentResponseDto>(responseBody);
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

            if (string.IsNullOrWhiteSpace(responseBody))
                throw new ApiException(500, "استجابة فارغة من الخادم");

            return DeserializeData<SubscriptionDto>(responseBody);
        }

        public async Task<SubscriptionPaymentVerificationDto> VerifyLatestSubscriptionPaymentAsync()
        {
            var response = await _httpClient.GetAsync(_verifyLatestPaymentUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ أثناء التحقق من الدفع" : combined);
            }

            if (string.IsNullOrWhiteSpace(responseBody))
                return new SubscriptionPaymentVerificationDto { Status = "none" };

            return DeserializeData<SubscriptionPaymentVerificationDto>(responseBody);
        }

        private static T DeserializeData<T>(string responseBody) where T : class
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
                var dataToken = obj?["data"] ?? obj?["Data"];
                if (dataToken != null)
                {
                    var dataJson = dataToken.ToString();
                    return JsonConvert.DeserializeObject<T>(dataJson, _jsonSettings) ?? throw new ApiException(500, "فشل تحليل بيانات الاستجابة");
                }
            }
            catch
            {
                // Not a JSON object wrapper — try direct deserialization below
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(responseBody, _jsonSettings) ?? throw new ApiException(500, "فشل تحليل بيانات الاستجابة");
            }
            catch
            {
                throw new ApiException(500, "فشل تحليل بيانات الاستجابة");
            }
        }

        public async Task<SubscriptionCancellationQuoteDto> GetMyCancellationQuoteAsync()
        {
            var response = await _httpClient.GetAsync(_cancellationQuoteUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب تفاصيل إلغاء الاشتراك" : combined);
            }

            if (string.IsNullOrWhiteSpace(responseBody))
                throw new ApiException(500, "استجابة فارغة من الخادم");

            return DeserializeData<SubscriptionCancellationQuoteDto>(responseBody);
        }

        private async Task<string> CancelInternalAsync(string? reason)
        {
            var payload = JsonConvert.SerializeObject(new { reason }, _jsonSettings);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_cancelUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في إلغاء الاشتراك" : combined);
            }

            return responseBody;
        }

        public async Task<CancelSubscriptionResponseDto> CancelMySubscriptionAsync(string? reason = null)
        {
            var responseBody = await CancelInternalAsync(reason);

            var dto = TryDeserializeData<CancelSubscriptionResponseDto>(responseBody);
            // Older backends answered with a bare `true`/message instead of the DTO.
            // The cancellation still succeeded, so return an empty-but-valid result
            // rather than failing the flow over an unreadable refund summary.
            return dto ?? new CancelSubscriptionResponseDto { CancelledAt = DateTime.UtcNow };
        }

        private static T? TryDeserializeData<T>(string responseBody) where T : class
        {
            if (string.IsNullOrWhiteSpace(responseBody)) return null;

            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
                var dataToken = obj?["data"] ?? obj?["Data"];
                if (dataToken is JObject dataObject)
                {
                    return JsonConvert.DeserializeObject<T>(dataObject.ToString(), _jsonSettings);
                }
            }
            catch
            {
                // Not a JSON object wrapper — nothing to extract.
            }

            return null;
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
