using System.Text;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Enums;
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
    public class UserService : IUserService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        private readonly string _getAllUsers;
        private readonly string _changePassword;
        private readonly string _createUser;
        private readonly Func<Guid, string> _deleteUser;
        private readonly Func<Guid, string> _updateUser;
        private readonly Func<Guid, string> _editUserRole;

        public UserService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _getAllUsers = DoctoryRoutes.Users.GetAll;
            _changePassword = DoctoryRoutes.Users.ChangePassword;
            _createUser = DoctoryRoutes.Users.Create;
            _deleteUser = DoctoryRoutes.Users.Delete;
            _updateUser = DoctoryRoutes.Users.EditUser;
            _editUserRole = DoctoryRoutes.Users.EditRole;
        }

        public async Task<Unit> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["Id"] = request.Id,
                    ["OldPassword"] = request.OldPassword,
                    ["NewPassword"] = request.NewPassword,
                    ["ConfirmPassword"] = request.ConfirmPassword
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_changePassword, content);
                return await _deserializerService.DeserializeApiResponse<Unit>(response, "حدث خطأ في تغيير كلمة المرور");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }


        public async Task<PagginatedResult<UserResponseDto>> GetAllUsersPagginatedAsync(GetAllUsersRequest request)
        {
            try
            {
                var url = $"{_getAllUsers}?PageNumber={request.PageNumber}&PageSize={request.PageSize}";
                if (request.UserTypes.Count > 0)
                {
                    foreach (var userType in request.UserTypes)
                        url += $"&UserTypes={(int)userType}";
                }
                else
                {
                    foreach (var userType in Enum.GetValues<UserType>())
                    {
                        if (userType != UserType.None)
                            url += $"&UserTypes={(int)userType}";
                    }
                }
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    url += $"&SearchTerm={Uri.EscapeDataString(request.SearchTerm)}";
                if (request.UserId.HasValue)
                    url += $"&UserId={request.UserId.Value}";
                if (request.IsUnassigned.HasValue)
                    url += $"&IsUnassigned={request.IsUnassigned.Value.ToString().ToLower()}";
                if (request.ClinicId.HasValue)
                    url += $"&ClinicId={request.ClinicId.Value}";

                var response = await _httpClient.GetAsync(url);
                return await _deserializerService.DeserializeApiResponse<PagginatedResult<UserResponseDto>>(response, "حدث خطأ في جلب المستخدمين");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<ApiResponse<Guid>> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var isClinicBoundRole = request.Role is UserType.Doctor or UserType.Staff or UserType.ClinicOwner;
                if (isClinicBoundRole && !request.ClinicId.HasValue)
                    throw new ApiException(400, "يجب اختيار العيادة للمستخدمين بهذا الدور");

                var payload = new Dictionary<string, object?>
                {
                    ["FullName"] = request.FullName,
                    ["Email"] = request.Email,
                    ["Password"] = request.Password,
                    ["PhoneNumber"] = request.PhoneNumber,
                    ["BirthDate"] = request.BirthDate,
                    ["Gender"] = (int)request.Gender,
                    ["Role"] = (int)request.Role
                };

                if (request.ClinicId.HasValue)
                    payload["ClinicId"] = request.ClinicId.Value;

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_createUser, content);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && Guid.TryParse(body.Trim('"'), out var plainId))
                    return new ApiResponse<Guid> { Success = true, Data = plainId };

                // The API answers with { success, data: { id } } and omits data on failure.
                // Deserializing straight into ApiResponse<Guid> threw on that shape and hid
                // the real validation message behind a JSON conversion error.
                var envelope = JsonConvert.DeserializeObject<JObject>(body);
                if (envelope?.Value<bool?>("success") == true)
                {
                    var dataToken = envelope["data"] ?? envelope["Data"];
                    var rawId = dataToken is JObject dataObject
                        ? dataObject.Value<string>("id") ?? dataObject.Value<string>("Id")
                        : dataToken?.ToString();

                    return new ApiResponse<Guid>
                    {
                        Success = true,
                        Data = Guid.TryParse(rawId, out var createdId) ? createdId : Guid.Empty
                    };
                }

                var errors = ApiErrorExtractor.ExtractErrors(body);
                var combined = string.Join("\n", errors);

                if (string.IsNullOrWhiteSpace(combined))
                    combined = envelope?.Value<string>("message") ?? envelope?.Value<string>("Message") ?? "حدث خطأ في إنشاء المستخدم";

                var statusCode = envelope?.Value<int?>("statusCode") is int apiStatus && apiStatus > 0
                    ? apiStatus
                    : (int)response.StatusCode;
                throw new ApiException(statusCode, combined);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteUserAsync(DeleteUserRequest request)
        {
            try
            {
                var url = _deleteUser(request.Id);
                var req = new HttpRequestMessage(HttpMethod.Delete, url);
                var response = await _httpClient.SendAsync(req);
                var success = await _deserializerService.DeserializeApiResponse<bool?>(response, "حدث خطأ في حذف المستخدم");
                return new ApiResponse<bool> { Success = true, Data = success ?? true };

            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> EditUserAsync(EditUserRequest request)
        {
            try
            {
                var url = _updateUser(request.Id);
                var payload = new Dictionary<string, object?>
                {
                    ["FullName"] = request.FullName,
                    ["PhoneNumber"] = request.PhoneNumber,
                    ["BirthDate"] = request.BirthDate,
                    ["Gender"] = request.Gender.HasValue ? (int)request.Gender.Value : null,
                    ["IsActive"] = request.IsActive
                };

                var json = JsonConvert.SerializeObject(payload, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var requestMessage = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(requestMessage);
                var success = await _deserializerService.DeserializeApiResponse<bool?>(response, "حدث خطأ في تعديل المستخدم");
                return new ApiResponse<bool> { Success = true, Data = success ?? true };
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> EditUserRoleAsync(EditUserRoleRequest request)
        {
            try
            {
                var url = _editUserRole(request.Id);
                var payload = new Dictionary<string, object?>
                {
                    ["NewRole"] = (int)request.Role
                };

                var json = JsonConvert.SerializeObject(payload, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // PUT replaces the whole role set; the API keeps the user's clinic for clinic-bound roles.
                var requestMessage = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(requestMessage);
                var success = await _deserializerService.DeserializeApiResponse<bool?>(response, "حدث خطأ في تغيير دور المستخدم");
                return new ApiResponse<bool> { Success = true, Data = success ?? true };
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
