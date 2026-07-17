using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IUserService
    {
        Task<PagginatedResult<UserResponseDto>> GetAllUsersPagginatedAsync(GetAllUsersRequest request);
    }
}
