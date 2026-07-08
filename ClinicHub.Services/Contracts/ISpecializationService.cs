using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface ISpecializationService
    {
        Task<PagginatedResult<SpecializationDto>> GetAllAsync(int pageNumber = 1, int pageSize = 20, bool? isFamous = null);
        Task<ApiResponse<SpecializationDto?>> GetByIdAsync(Guid id);
        Task<string> CreateAsync(CreateSpecializationRequest request);
        Task<SpecializationDto?> UpdateAsync(UpdateSpecializationRequest request);
        Task<string> DeleteAsync(DeleteSpecializationRequest request);
    }
}
