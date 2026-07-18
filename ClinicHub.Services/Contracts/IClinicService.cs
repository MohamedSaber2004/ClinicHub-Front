using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IClinicService
    {
        Task<ApiResponse<List<ClinicLookupDto>>> GetAllClinicsForViewingOnlyAsync(GetAllCLinicsForViewingOnly request);
    }
}
