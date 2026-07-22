using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IClinicService
    {
        Task<ApiResponse<List<ClinicLookupDto>>> GetAllClinicsForViewingOnlyAsync(GetAllCLinicsForViewingOnly request);

        Task<PagginatedResult<ClinicManagmentDto>> GetAllClinicsPaginatedAsync(GetAllClinicsPagginatedRequest request);

        Task<ApiResponse<ClinicManagmentDto>> GetClinicByIdAsync(GetClinicByIdRequest request);

        Task<ApiResponse<ClinicManagmentDto>> CreateClinicAsync(CreateClinicRequest request);

        Task<ApiResponse<ClinicManagmentDto>> UpdateClinicAsync(UpdateClinicRequest request);
    }
}
