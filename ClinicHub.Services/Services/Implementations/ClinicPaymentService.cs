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
    public class ClinicPaymentService : IClinicPaymentService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly string _listUrl;
        private readonly string _statsUrl;

        public ClinicPaymentService(HttpClient httpClient, IOptions<Doctory> doctoryOptions)
        {
            _httpClient = httpClient;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
            _listUrl = DoctoryRoutes.AppointmentPayments.Appointments;
            _statsUrl = DoctoryRoutes.AppointmentPayments.Stats;
        }

        public async Task<PagginatedResult<AppointmentPaymentDto>> GetAppointmentPaymentsAsync(int? status = null, int? method = null, int pageNumber = 1, int pageSize = 10)
        {
            var query = new List<string>
            {
                $"pageNumber={pageNumber}",
                $"pageSize={pageSize}"
            };
            if (status.HasValue) query.Add($"status={status.Value}");
            if (method.HasValue) query.Add($"method={method.Value}");

            var response = await _httpClient.GetAsync($"{_listUrl}?{string.Join("&", query)}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "عذراً، حدث خطأ أثناء تحميل إيرادات المواعيد." : combined);
            }

            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
                var dataToken = obj?["data"] ?? obj?["Data"];
                var dataJson = dataToken?.ToString() ?? responseBody;
                return JsonConvert.DeserializeObject<PagginatedResult<AppointmentPaymentDto>>(dataJson, _jsonSettings)
                       ?? PagginatedResult<AppointmentPaymentDto>.Create(new List<AppointmentPaymentDto>(), 0, pageNumber, pageSize);
            }
            catch
            {
                return PagginatedResult<AppointmentPaymentDto>.Create(new List<AppointmentPaymentDto>(), 0, pageNumber, pageSize);
            }
        }

        public async Task<AppointmentRevenueStatsDto> GetAppointmentRevenueStatsAsync()
        {
            var response = await _httpClient.GetAsync(_statsUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "عذراً، حدث خطأ أثناء تحميل إحصائيات الإيرادات." : combined);
            }

            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
                var dataToken = obj?["data"] ?? obj?["Data"];
                var dataJson = dataToken?.ToString() ?? responseBody;
                return JsonConvert.DeserializeObject<AppointmentRevenueStatsDto>(dataJson, _jsonSettings)
                       ?? new AppointmentRevenueStatsDto();
            }
            catch
            {
                return new AppointmentRevenueStatsDto();
            }
        }
    }
}
