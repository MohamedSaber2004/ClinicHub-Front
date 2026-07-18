using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Services.Implementations
{
    public class DoctorService : IDoctorService
    {
        private readonly IUserService _userService;

        public DoctorService(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<PagginatedResult<UserResponseDto>> GetAllDoctorsPagginatedAsync(GetAllDoctorsRequest request)
        {
            try
            {
                var usersRequest = new GetAllUsersRequest
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SearchTerm = request.SearchTerm,
                    UserTypes = new() { Enums.UserType.Doctor, Enums.UserType.ClinicOwner },
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
