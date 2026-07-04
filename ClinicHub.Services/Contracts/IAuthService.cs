using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequest request);
    }
}
