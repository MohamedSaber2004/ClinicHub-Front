using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using ClinicHub.Services.Routes.Api;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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

        public UserService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _getAllUsers = DoctoryRoutes.Users.GetAll;
        }

        public async Task<PagginatedResult<UserResponseDto>> GetAllUsersPagginatedAsync(GetAllUsersRequest request)
        {
            try
            {
                var url = $"{_getAllUsers}?PageNumber={request.PageNumber}&PageSize={request.PageSize}&UserType={(int)Enums.UserType.User}";
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    url += $"&SearchTerm={Uri.EscapeDataString(request.SearchTerm)}";
                if (request.UserId.HasValue)
                    url += $"&UserId={request.UserId.Value}";

                var response = await _httpClient.GetAsync(url);
                return await _deserializerService.DeserializeApiResponse<PagginatedResult<UserResponseDto>>(response, "حدث خطأ في جلب المستخدمين");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
