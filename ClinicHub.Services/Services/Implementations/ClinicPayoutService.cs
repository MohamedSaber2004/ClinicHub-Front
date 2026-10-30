using System.Text;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using ClinicHub.Services.Routes.Api;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Services.Implementations
{
    public class ClinicPayoutService : IClinicPayoutService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;
        private readonly string _accountUrl;
        private readonly string _summaryUrl;
        private readonly string _historyUrl;

        public ClinicPayoutService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
            _accountUrl = DoctoryRoutes.ClinicPayouts.Account;
            _summaryUrl = DoctoryRoutes.ClinicPayouts.Summary;
            _historyUrl = DoctoryRoutes.ClinicPayouts.History;
        }

        public async Task<ClinicPayoutSummaryDto> GetSummaryAsync()
        {
            var response = await _httpClient.GetAsync(_summaryUrl);
            return await _deserializerService.DeserializeApiResponse<ClinicPayoutSummaryDto>(
                response, "عذراً، حدث خطأ أثناء تحميل مستحقات العيادة.");
        }

        public async Task<ClinicPayoutAccountDto?> GetAccountAsync()
        {
            var response = await _httpClient.GetAsync(_accountUrl);
            return await _deserializerService.DeserializeApiResponse<ClinicPayoutAccountDto?>(
                response, "عذراً، حدث خطأ أثناء تحميل الحساب البنكي.");
        }

        public async Task<ClinicPayoutAccountDto> UpsertAccountAsync(UpsertPayoutAccountRequest request)
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(request, _jsonSettings),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync(_accountUrl, content);
            return await _deserializerService.DeserializeApiResponse<ClinicPayoutAccountDto>(
                response, "تعذر حفظ الحساب البنكي، يرجى المحاولة لاحقاً.");
        }

        public async Task<PagginatedResult<PayoutItemDto>> GetHistoryAsync(int pageNumber = 1, int pageSize = 20)
        {
            var response = await _httpClient.GetAsync(
                $"{_historyUrl}?pageNumber={pageNumber}&pageSize={pageSize}");
            return await _deserializerService.DeserializeApiResponse<PagginatedResult<PayoutItemDto>>(
                response, "عذراً، حدث خطأ أثناء تحميل كشف التحويلات.");
        }
    }
}
