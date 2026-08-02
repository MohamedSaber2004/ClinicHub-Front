using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IAdminPaymentService
    {
        Task<PagginatedResult<AdminPaymentDto>> GetPaymentsAsync(GetAdminPaymentsRequest request);
        Task<PaymentDetailDto> GetPaymentDetailAsync(Guid id);
        Task<PaymentStatsDto> GetPaymentStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<AdminPaymentDto> CreateManualPaymentAsync(CreateManualPaymentRequest request);
        Task<bool> RefundPaymentAsync(Guid id, string? reason);
        Task<List<EligibleClinicDto>> GetEligibleClinicsAsync();
        Task<List<AdPackageDto>> GetAdPackagesAsync();
        Task<AdsOrderResponseDto> CreateAdsOrderAsync(CreateAdsOrderRequest request);
    }
}
