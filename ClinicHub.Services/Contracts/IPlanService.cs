using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Services.Contracts
{
    public interface IPlanService
    {
        Task<List<PlanDto>> GetAllAsync();
        Task<ApiResponse<PlanDto>> GetByIdAsync(Guid id);
    }
}
