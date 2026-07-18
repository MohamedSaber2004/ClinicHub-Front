using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using Microsoft.AspNetCore.Http;

namespace ClinicHub.Services.Services.Implementations
{
    public class DoctorService : IDoctorService
    {
        private readonly IUserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DoctorService(IUserService userService, IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PagginatedResult<UserResponseDto>> GetAllDoctorsPagginatedAsync(GetAllDoctorsRequest request)
        {
            try
            {
                var hasClinicFilter = _httpContextAccessor.HttpContext?.Items["ClinicId"] is Guid;
                var isFiltering = request.IsUnassigned is true || hasClinicFilter;

                var userTypes = new List<Enums.UserType> { Enums.UserType.Doctor };
                if (!isFiltering)
                    userTypes.Add(Enums.UserType.ClinicOwner);

                var usersRequest = new GetAllUsersRequest
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SearchTerm = request.SearchTerm,
                    UserTypes = userTypes,
                    IsUnassigned = request.IsUnassigned
                };

                return await _userService.GetAllUsersPagginatedAsync(usersRequest);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
