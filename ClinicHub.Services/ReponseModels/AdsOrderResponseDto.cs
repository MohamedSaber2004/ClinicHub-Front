using Newtonsoft.Json;

namespace ClinicHub.Services.ReponseModels
{
    public class AdsOrderResponseDto
    {
        public Guid PaymentId { get; set; }
        public string RefNumber { get; set; } = "";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public int Status { get; set; }
        public string? PaymobRedirectUrl { get; set; }
        public string? PaymobPaymentKey { get; set; }

        [JsonIgnore]
        public string TargetRedirectUrl =>
            !string.IsNullOrWhiteSpace(PaymobRedirectUrl) ? PaymobRedirectUrl : string.Empty;
    }
}
