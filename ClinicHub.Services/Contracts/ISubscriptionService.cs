using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface ISubscriptionService
    {
        Task<InitiatePaymentResponseDto> InitiatePaymentAsync(InitiatePaymentRequest request);
        Task<SubscriptionDto> GetMySubscriptionAsync();
        Task<string> CancelMySubscriptionAsync();
        Task<RegisterClinicResponseDto> RegisterClinicAsync(RegisterClinicRequest request);
    }
}
