using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using MediatR;

namespace ClinicHub.Services.Contracts
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequest request);
        Task<string> LogoutAsync(LogoutRequest request);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequest request);
        Task<Unit> ForgetPasswordAsync(ForgetPasswordRequest request);
        Task<bool> VerifyResetTokenAsync(VerifyResetTokenRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
