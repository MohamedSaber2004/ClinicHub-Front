using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IClinicService
    {
        Task<ApiResponse<List<ClinicLookupDto>>> GetAllClinicsForViewingOnlyAsync(GetAllCLinicsForViewingOnly request);

        Task<PagginatedResult<ClinicManagmentDto>> GetAllClinicsPaginatedAsync(GetAllClinicsPagginatedRequest request);

        Task<ApiResponse<ClinicManagmentDto>> GetClinicByIdAsync(GetClinicByIdRequest request);

        Task<ApiResponse<ClinicDetailsDto>> GetClinicDetailsAsync(GetClinicByIdRequest request);

        Task<ApiResponse<ClinicManagmentDto>> CreateClinicAsync(CreateClinicRequest request);

        Task<ApiResponse<ClinicManagmentDto>> UpdateClinicAsync(UpdateClinicRequest request);

        Task<ApiResponse<ClinicManagmentDto>> ActivateClinicAsync(ActivateClinicRequest request);

        Task<ApiResponse<ClinicManagmentDto>> DeactivateClinicAsync(DeactivateClinicRequest request);

        Task<ApiResponse<ClinicSettingsDto>> GetClinicSettingsAsync();

        Task<ApiResponse<ClinicSettingsDto>> UpdateClinicSettingsAsync(UpdateClinicSettingsRequest request);
    }
}
