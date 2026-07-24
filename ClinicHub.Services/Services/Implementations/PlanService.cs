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
    public class PlanService : IPlanService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly string _listUrl;

        public PlanService(HttpClient httpClient, IOptions<Doctory> doctoryOptions)
        {
            _httpClient = httpClient;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
            _listUrl = DoctoryRoutes.Plans.List;
        }

        public async Task<List<PlanDto>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync(_listUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب الباقات" : combined);
            }

            if (string.IsNullOrWhiteSpace(responseBody)) return new();

            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
                var dataToken = obj?["data"] ?? obj?["Data"];
                if (dataToken != null)
                {
                    var dataJson = dataToken.ToString();
                    return JsonConvert.DeserializeObject<List<PlanDto>>(dataJson, _jsonSettings) ?? new();
                }
            }
            catch
            {
                // Not a JSON object wrapper — try direct array deserialization below
            }

            try
            {
                return JsonConvert.DeserializeObject<List<PlanDto>>(responseBody, _jsonSettings) ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<ApiResponse<PlanDto>> GetByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"{_listUrl}/{id}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب الباقة" : combined);
            }

            var result = JsonConvert.DeserializeObject<ApiResponse<PlanDto>>(responseBody, _jsonSettings);
            return result!;
        }
    }
}
