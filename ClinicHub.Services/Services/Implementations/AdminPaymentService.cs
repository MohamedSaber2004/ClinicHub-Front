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
    public class AdminPaymentService : IAdminPaymentService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public AdminPaymentService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<PagginatedResult<AdminPaymentDto>> GetPaymentsAsync(GetAdminPaymentsRequest request)
        {
            try
            {
                var url = $"{DoctoryRoutes.AdminPayments.List}?PageNumber={request.PageNumber}&PageSize={request.PageSize}";
                if (request.Type.HasValue) url += $"&Type={request.Type.Value}";
                if (request.Status.HasValue) url += $"&Status={request.Status.Value}";
                if (request.Method.HasValue) url += $"&Method={request.Method.Value}";
                if (request.FromDate.HasValue) url += $"&FromDate={request.FromDate.Value:yyyy-MM-dd}";
                if (request.ToDate.HasValue) url += $"&ToDate={request.ToDate.Value:yyyy-MM-dd}";
                if (!string.IsNullOrWhiteSpace(request.SearchTerm)) url += $"&SearchTerm={Uri.EscapeDataString(request.SearchTerm)}";

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
                return JsonConvert.DeserializeObject<PagginatedResult<AdminPaymentDto>>(dataJson, _jsonSettings)
                    ?? new PagginatedResult<AdminPaymentDto>(new List<AdminPaymentDto>(), 0);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<PaymentDetailDto> GetPaymentDetailAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.AdminPayments.Detail(id));
                return await _deserializerService.DeserializeApiResponse<PaymentDetailDto>(response, "حدث خطأ في جلب تفاصيل المعاملة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<PaymentStatsDto> GetPaymentStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var url = DoctoryRoutes.AdminPayments.Stats;
                if (fromDate.HasValue) url += $"?FromDate={fromDate.Value:yyyy-MM-dd}";
                if (toDate.HasValue) url += $"{(fromDate.HasValue ? "&" : "?")}ToDate={toDate.Value:yyyy-MM-dd}";

                var response = await _httpClient.GetAsync(url);
                return await _deserializerService.DeserializeApiResponse<PaymentStatsDto>(response, "حدث خطأ في جلب إحصائيات المدفوعات");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<AdminPaymentDto> CreateManualPaymentAsync(CreateManualPaymentRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.AdminPayments.Manual, content);
                return await _deserializerService.DeserializeApiResponse<AdminPaymentDto>(response, "حدث خطأ في تسجيل الدفعة اليدوية");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> RefundPaymentAsync(Guid id, string? reason)
        {
            try
            {
                var json = JsonConvert.SerializeObject(new { reason }, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.AdminPayments.Refund(id), content);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في استرداد المبلغ");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<EligibleClinicDto>> GetEligibleClinicsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.AdminAds.EligibleClinics);
                return await _deserializerService.DeserializeApiResponse<List<EligibleClinicDto>>(response, "حدث خطأ في جلب العيادات المؤهلة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<AdPackageDto>> GetAdPackagesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.AdminAds.Packages);
                return await _deserializerService.DeserializeApiResponse<List<AdPackageDto>>(response, "حدث خطأ في جلب باقات الإعلانات");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<AdsOrderResponseDto> CreateAdsOrderAsync(CreateAdsOrderRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.AdminAds.Orders, content);
                return await _deserializerService.DeserializeApiResponse<AdsOrderResponseDto>(response, "حدث خطأ في إنشاء طلب الإعلان");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
