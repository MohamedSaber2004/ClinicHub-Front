using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Services.Contracts
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardStatsDto> GetStatsAsync();
        Task<List<SupportTicketDto>> GetUrgentTicketsAsync();
        Task<PagginatedResult<SubscriptionDto>> GetSubscriptionsAsync(int pageNumber = 1, int pageSize = 5);
        Task<PagginatedResult<SupportTicketDto>> GetTicketsAsync(int? status = null, int? priority = null, int pageNumber = 1, int pageSize = 20);
        Task<bool> UpdateTicketStatusAsync(Guid id, int status);
    }
}
