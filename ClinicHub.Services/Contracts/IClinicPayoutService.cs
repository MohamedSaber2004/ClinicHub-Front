using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IClinicPayoutService
    {
        Task<ClinicPayoutSummaryDto> GetSummaryAsync();
        Task<ClinicPayoutAccountDto?> GetAccountAsync();
        Task<ClinicPayoutAccountDto> UpsertAccountAsync(UpsertPayoutAccountRequest request);
        Task<PagginatedResult<PayoutItemDto>> GetHistoryAsync(int pageNumber = 1, int pageSize = 20);
    }
}
