using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IAdminPaymentService
    {
        Task<PagginatedResult<AdminPaymentDto>> GetPaymentsAsync(GetAdminPaymentsRequest request);
        Task<PagginatedResult<ClinicPaymentsSummaryDto>> GetClinicsSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null, int pageNumber = 1, int pageSize = 20);
        Task<PaymentDetailDto> GetPaymentDetailAsync(Guid id);
        Task<PaymentStatsDto> GetPaymentStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<AdminPaymentDto> CreateManualPaymentAsync(CreateManualPaymentRequest request);
        Task<bool> RefundPaymentAsync(Guid id, string? reason);
        Task<List<EligibleClinicDto>> GetEligibleClinicsAsync();
        Task<List<AdPackageDto>> GetAdPackagesAsync();
        Task<AdsOrderResponseDto> CreateAdsOrderAsync(CreateAdsOrderRequest request);
    }
}
