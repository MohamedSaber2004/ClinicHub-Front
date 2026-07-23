using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IAdminSubscriptionService
    {
        Task<PagginatedResult<SubscriptionDto>> GetSubscriptionsAsync(GetPaginatedSubscriptionsRequest request);
        Task<string> RevokeSubscriptionAsync(RevokeSubscriptionRequest request);
        Task<List<PlanDto>> GetAllPlansAsync();
        Task<PlanDto> CreatePlanAsync(PlanDto plan);
        Task<PlanDto> UpdatePlanAsync(Guid id, PlanDto plan);
        Task<string> DeletePlanAsync(Guid id);
    }
}
