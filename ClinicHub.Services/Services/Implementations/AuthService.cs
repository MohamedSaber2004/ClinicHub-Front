using System.Text;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using ClinicHub.Services.Routes.Api;
using ClinicHub.Services.Utilities;
using MediatR;
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
        private readonly string _logoutUrl;
        private readonly string _refreshTokenUrl;
        private readonly string _forgetPasswordUrl;
        private readonly string _verifyResetTokenUrl;
        private readonly string _resetPasswordUrl;

        public AuthService(HttpClient httpClient, IOptions<Doctory> doctoryOptions)
        {
            _httpClient = httpClient;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _loginUrl = DoctoryRoutes.Auth.Login;
            _logoutUrl = DoctoryRoutes.Auth.Logout;
            _refreshTokenUrl = DoctoryRoutes.Auth.RefreshToken;
            _forgetPasswordUrl = DoctoryRoutes.Auth.ForgetPassword;
            _verifyResetTokenUrl = DoctoryRoutes.Auth.VerifyResetToken;
            _resetPasswordUrl = DoctoryRoutes.Auth.ResetPassword;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequest request)
        {
            var json = JsonConvert.SerializeObject(request, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_loginUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];

            AuthResponseDto? result = null;
            if (dataToken != null)
            {
                var dataJson = dataToken.ToString();
                result = JsonConvert.DeserializeObject<AuthResponseDto>(dataJson, _jsonSettings);
            }
            else
            {
                result = JsonConvert.DeserializeObject<AuthResponseDto>(responseBody, _jsonSettings);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);

                if (result == null || (string.IsNullOrWhiteSpace(result.AccessToken) && string.IsNullOrWhiteSpace(result.RefreshToken)))
                    throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في تسجيل الدخول" : combined);

                throw new AuthenticatedApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في تسجيل الدخول" : combined, result);
            }

            return result!;
        }

        public async Task<string> LogoutAsync(LogoutRequest request)
        {
            var json  = JsonConvert.SerializeObject(request, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_logoutUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في تسجيل الخروج" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];

            var dataJson = dataToken?.ToString() ?? responseBody;
            var result = JsonConvert.DeserializeObject<string>(dataJson, _jsonSettings);

            return result!;
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var json = JsonConvert.SerializeObject(request, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_refreshTokenUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في تحديث الجلسة" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];

            var dataJson = dataToken?.ToString() ?? responseBody;
            var result = JsonConvert.DeserializeObject<AuthResponseDto>(dataJson, _jsonSettings);

            return result!;
        }

        public async Task<Unit> ForgetPasswordAsync(ForgetPasswordRequest request)
        {
            var json = JsonConvert.SerializeObject(request, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_forgetPasswordUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في إرسال رمز التحقق" : combined);
            }

            return Unit.Value;
        }

        public async Task<bool> VerifyResetTokenAsync(VerifyResetTokenRequest request)
        {
            var json = JsonConvert.SerializeObject(request, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_verifyResetTokenUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "رمز التحقق غير صحيح أو منتهي الصلاحية" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];

            if (dataToken != null && dataToken.Type == JTokenType.Boolean)
                return dataToken.Value<bool>();

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var json = JsonConvert.SerializeObject(request, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_resetPasswordUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessages = ApiErrorExtractor.ExtractErrors(responseBody);
                var combined = string.Join(" ", errorMessages);
                throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في إعادة تعيين كلمة المرور" : combined);
            }

            var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
            var dataToken = obj?["data"] ?? obj?["Data"];

            if (dataToken != null && dataToken.Type == JTokenType.Boolean)
                return dataToken.Value<bool>();

            return true;
        }
    }
}
