using System.Text;
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
    public class DoctorService : IDoctorService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly IUserService _userService;
        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public DoctorService(IUserService userService, HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _userService = userService;
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<PagginatedResult<UserResponseDto>> GetAllDoctorsPagginatedAsync(GetAllDoctorsRequest request)
        {
            try
            {
                var userTypes = new List<Enums.UserType> { Enums.UserType.Doctor };
                if (!request.ClinicId.HasValue)
                    userTypes.Add(Enums.UserType.ClinicOwner);

                var usersRequest = new GetAllUsersRequest
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SearchTerm = request.SearchTerm,
                    UserTypes = request.UserTypes?.Count > 0 ? request.UserTypes : userTypes,
                    IsUnassigned = request.IsUnassigned,
                    ClinicId = request.ClinicId
                };

                return await _userService.GetAllUsersPagginatedAsync(usersRequest);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<DoctorAvailabilityDto>> GetMyAvailabilityAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.Doctors.Availability);
                return await _deserializerService.DeserializeApiResponse<List<DoctorAvailabilityDto>>(response, "حدث خطأ في جلب أوقات العمل");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<DoctorAvailabilityDto>> ReplaceWeeklyAvailabilityAsync(ReplaceWeeklyAvailabilityRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(DoctoryRoutes.Doctors.AvailabilityWeek, content);
                return await _deserializerService.DeserializeApiResponse<List<DoctorAvailabilityDto>>(response, "حدث خطأ في حفظ أوقات العمل");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
