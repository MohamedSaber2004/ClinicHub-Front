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
    public class NotificationService : INotificationService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly string _countUrl;
        private readonly string _listUrl;

        public NotificationService(HttpClient httpClient, IOptions<Doctory> doctoryOptions)
        {
            _httpClient = httpClient;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
            _countUrl = DoctoryRoutes.Notifications.Count;
            _listUrl = DoctoryRoutes.Notifications.List;
        }

        public async Task<int> GetUnreadCountAsync()
        {
            var response = await _httpClient.GetAsync(_countUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب عدد الإشعارات" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];
            if (dataToken != null && dataToken.Type == JTokenType.Integer)
                return dataToken.Value<int>();

            return 0;
        }

        public async Task<PagginatedResult<NotificationDto>> GetNotificationsAsync(int pageNumber, int pageSize)
        {
            var url = $"{_listUrl}?pageNumber={pageNumber}&pageSize={pageSize}";
            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في جلب الإشعارات" : combined);
            }

            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
                var dataToken = obj?["data"] ?? obj?["Data"];
                var dataJson = dataToken?.ToString() ?? responseBody;
                return JsonConvert.DeserializeObject<PagginatedResult<NotificationDto>>(dataJson, _jsonSettings)
                       ?? PagginatedResult<NotificationDto>.Create(new List<NotificationDto>(), 0, pageNumber, pageSize);
            }
            catch
            {
                return PagginatedResult<NotificationDto>.Create(new List<NotificationDto>(), 0, pageNumber, pageSize);
            }
        }
    }
}
