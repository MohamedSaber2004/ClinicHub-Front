using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IUserVerificationService
    {
        Task<PagginatedResult<UserVerficationDto>> GetPendingVerificationsAsync(GetPendingVerficationsRequest request);
        Task<ApiResponse<bool>> ApproveUserVerificationAsync(ApproveUserVerficationRequest request);
        Task<ApiResponse<bool>> RejectUserVerificationAsync(RejectUserVerificationRequest request);
    }
}
