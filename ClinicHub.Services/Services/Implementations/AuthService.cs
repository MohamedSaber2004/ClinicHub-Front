using System.Text;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using ClinicHub.Services.Routes.Api;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly string _loginUrl;

        public AuthService(IOptions<Doctory> doctoryOptions)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
            _loginUrl = DoctoryRoutes.Auth.Login;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequest request)
        {
            var json = JsonConvert.SerializeObject(request, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_loginUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ExtractApiErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new Exception(string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في تسجيل الدخول" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"] ?? obj?["result"] ?? obj?["Result"];

            var dataJson = dataToken?.ToString() ?? responseBody;
            var result = JsonConvert.DeserializeObject<AuthResponseDto>(dataJson, _jsonSettings);

            return result!;
        }

        private static List<string> ExtractApiErrors(string body)
        {
            var errors = new List<string>();

            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(body);
                if (obj == null) return errors;

                var message = obj["message"]?.ToString() ?? obj["Message"]?.ToString() ?? "";

                var errorsToken = obj["errors"] ?? obj["Errors"];
                if (errorsToken != null && errorsToken.Type == JTokenType.Object)
                {
                    foreach (var prop in errorsToken.Children<JProperty>())
                    {
                        if (prop.Value.Type == JTokenType.Array)
                        {
                            foreach (var item in prop.Value)
                            {
                                var msg = item.ToString();
                                if (!string.IsNullOrWhiteSpace(msg))
                                    errors.Add(msg);
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    if (errors.Count == 0)
                        errors.Add(message);
                    else
                        errors.Insert(0, message);
                }
            }
            catch
            {
                // fallback — keep errors empty
            }

            return errors;
        }
    }
}
