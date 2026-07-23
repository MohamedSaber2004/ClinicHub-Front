using System;
using Newtonsoft.Json;

namespace ClinicHub.Services.ReponseModels
{
    public class InitiatePaymentResponseDto
    {
        public Guid PaymentId { get; set; }

        [JsonProperty("paymobRedirectUrl")]
        public string? PaymobRedirectUrl { get; set; }

        [JsonProperty("redirectUrl")]
        public string? RedirectUrl { get; set; }

        [JsonProperty("paymentUrl")]
        public string? PaymentUrl { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonIgnore]
        public string TargetRedirectUrl =>
            !string.IsNullOrWhiteSpace(PaymobRedirectUrl) ? PaymobRedirectUrl :
            !string.IsNullOrWhiteSpace(RedirectUrl) ? RedirectUrl :
            !string.IsNullOrWhiteSpace(PaymentUrl) ? PaymentUrl :
            !string.IsNullOrWhiteSpace(Url) ? Url : string.Empty;

        public string? PaymobPaymentKey { get; set; }
        public Guid PlanId { get; set; }
        public string? PlanName { get; set; }
        public int Period { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
    }
}
