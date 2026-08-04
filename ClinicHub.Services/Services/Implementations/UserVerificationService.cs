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
    public class UserVerificationService : IUserVerificationService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;
        private readonly IAttachmentUrlResolver _attachmentUrlResolver;

        private readonly string _getPendingVerificationsRoutes;
        private readonly Func<Guid, string> _approveUserVerifications;
        private readonly Func<Guid, string> _rejectUserVerifications;

        public UserVerificationService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService, IAttachmentUrlResolver attachmentUrlResolver)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            _attachmentUrlResolver = attachmentUrlResolver;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _getPendingVerificationsRoutes = DoctoryRoutes.Verification.GetPendingVerifications;
            _approveUserVerifications = DoctoryRoutes.Verification.ApproveUserVerification;
            _rejectUserVerifications = DoctoryRoutes.Verification.RejectUserVerification;
        }

        public async Task<PagginatedResult<UserVerficationDto>> GetPendingVerificationsAsync(GetPendingVerficationsRequest request)
        {
            try
            {
                var url = $"{_getPendingVerificationsRoutes}?PageNumber={request.PageNumber}&PageSize={request.PageSize}";
                var response = await _httpClient.GetAsync(url);
                var result = await _deserializerService.DeserializeApiResponse<PagginatedResult<UserVerficationDto>>(response, "حدث خطأ في جلب طلبات التحقق");

                foreach (var item in result.Items)
                {
                    if (!string.IsNullOrWhiteSpace(item.DoctorImage) && !Uri.TryCreate(item.DoctorImage, UriKind.Absolute, out _))
                        item.DoctorImage = _attachmentUrlResolver.Resolve(item.DoctorImage);
                    if (!string.IsNullOrWhiteSpace(item.ProfessionalPracticeCardImage) && !Uri.TryCreate(item.ProfessionalPracticeCardImage, UriKind.Absolute, out _))
                        item.ProfessionalPracticeCardImage = _attachmentUrlResolver.Resolve(item.ProfessionalPracticeCardImage);
                    if (!string.IsNullOrWhiteSpace(item.TaxCardImage) && !Uri.TryCreate(item.TaxCardImage, UriKind.Absolute, out _))
                        item.TaxCardImage = _attachmentUrlResolver.Resolve(item.TaxCardImage);
                    if (!string.IsNullOrWhiteSpace(item.UnionIdCardImage) && !Uri.TryCreate(item.UnionIdCardImage, UriKind.Absolute, out _))
                        item.UnionIdCardImage = _attachmentUrlResolver.Resolve(item.UnionIdCardImage);
                }

                return result;
            }
            catch (ApiException) { throw; }

        }

        public async Task<ApiResponse<bool>> ApproveUserVerificationAsync(ApproveUserVerficationRequest request)
        {
            try
            {
                var payload = JsonConvert.SerializeObject(request);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_approveUserVerifications(request.UserId), content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    var combined = string.Join(" ", errors);
                    throw new ApiException((int)response.StatusCode,
                        string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في قبول طلب التحقق" : combined);
                }

                return ParseBoolResponse(body, (int)response.StatusCode, "تم قبول طلب التحقق بنجاح");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }


        public async Task<ApiResponse<bool>> RejectUserVerificationAsync(RejectUserVerificationRequest request)
        {
            try
            {
                var payload = JsonConvert.SerializeObject(request);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_rejectUserVerifications(request.UserId), content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    var combined = string.Join(" ", errors);
                    throw new ApiException((int)response.StatusCode,
                        string.IsNullOrWhiteSpace(combined) ? "حدث خطأ في رفض طلب التحقق" : combined);
                }

                return ParseBoolResponse(body, (int)response.StatusCode, "تم رفض طلب التحقق بنجاح");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        private static ApiResponse<bool> ParseBoolResponse(string body, int statusCode, string successMessage)
        {
            var fallback = new ApiResponse<bool> { Success = false, Message = "فشل في تحليل الاستجابة", StatusCode = statusCode };

            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(body);
                if (obj == null) return fallback;

                var success = obj["success"]?.Type == JTokenType.Boolean && obj["success"]!.Value<bool>();
                var message = obj["message"]?.ToString() ?? obj["Message"]?.ToString();

                return new ApiResponse<bool>
                {
                    Success = success,
                    Message = string.IsNullOrWhiteSpace(message) ? (success ? successMessage : null) : message,
                    StatusCode = statusCode,
                    Data = success
                };
            }
            catch
            {
                return fallback;
            }
        }
    }
}
