using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using MediatR;

namespace ClinicHub.Services.Contracts
{
    public interface IUserService
    {
        Task<PagginatedResult<UserResponseDto>> GetAllUsersPagginatedAsync(GetAllUsersRequest request);

        Task<Unit> ChangePasswordAsync(ChangePasswordRequest request);

        Task<ApiResponse<Guid>> CreateUserAsync(CreateUserRequest request);

        Task<ApiResponse<bool>> DeleteUserAsync(DeleteUserRequest request);

        Task<ApiResponse<bool>> EditUserAsync(EditUserRequest request);
    }
}
