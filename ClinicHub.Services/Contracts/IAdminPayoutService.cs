using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IAdminPayoutService
    {
        Task<PayoutPreviewDto> GetPreviewAsync(decimal? minimumAmount = null);
        Task<List<PayoutAccountAdminDto>> GetAccountsAsync(int? status = null, string? search = null);
        Task<PayoutAccountAdminDto> VerifyAccountAsync(Guid id);
        Task<PayoutAccountAdminDto> RejectAccountAsync(Guid id, string reason);
        Task<PayoutAccountAdminDto> UpsertClinicAccountAsync(AdminUpsertPayoutAccountRequest request);
        Task<ClinicLedgerCreditDto> CreditClinicAsync(CreditClinicLedgerRequest request);

        Task<PayoutBudgetDto> GetBudgetAsync();
        Task<PagginatedResult<PayoutBatchDto>> GetBatchesAsync(int pageNumber = 1, int pageSize = 20, int? status = null);
        Task<PayoutBatchDto> GetBatchAsync(Guid id);
        Task<PayoutBatchDto> CreateBatchAsync(CreatePayoutBatchRequest request);
        Task<PayoutBatchDto> ApproveBatchAsync(Guid id);
        Task<PayoutBatchDto> ExecuteBatchAsync(Guid id);
        Task<PayoutBatchDto> CancelBatchAsync(Guid id);
        Task<PayoutBatchDto> ReconcileBatchAsync(Guid id);
        Task<PayoutBatchDto> RetryItemAsync(Guid itemId);
    }
}
