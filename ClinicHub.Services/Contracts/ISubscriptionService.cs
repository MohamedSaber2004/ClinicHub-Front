using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface ISubscriptionService
    {
        Task<InitiatePaymentResponseDto> InitiatePaymentAsync(InitiatePaymentRequest request);
        Task<SubscriptionDto> GetMySubscriptionAsync();
        Task<SubscriptionCancellationQuoteDto> GetMyCancellationQuoteAsync();
        Task<CancelSubscriptionResponseDto> CancelMySubscriptionAsync(string? reason = null);
        Task<RegisterClinicResponseDto> RegisterClinicAsync(RegisterClinicRequest request);
        Task<SubscriptionPaymentVerificationDto> VerifyLatestSubscriptionPaymentAsync();
    }
}
