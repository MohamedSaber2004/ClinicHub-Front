using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    /// <summary>
    /// Payment envelope returned by accept/approve commands (appointment request → payment flow).
    /// Contract: docs/appointment-request-payment-flow.md §3.3.
    /// Older backends may still return <c>data: true</c> — services parse both shapes.
    /// </summary>
    public class AppointmentAcceptResponseDto
    {
        [JsonPropertyName("appointmentId")] [JsonProperty("appointmentId")] public string? AppointmentId { get; set; }
        [JsonPropertyName("status")] [JsonProperty("status")] public int? Status { get; set; }
        [JsonPropertyName("paymentId")] [JsonProperty("paymentId")] public string? PaymentId { get; set; }
        [JsonPropertyName("amount")] [JsonProperty("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("currency")] [JsonProperty("currency")] public string? Currency { get; set; }
        [JsonPropertyName("paymobRedirectUrl")] [JsonProperty("paymobRedirectUrl")] public string? PaymobRedirectUrl { get; set; }
        [JsonPropertyName("paymobPaymentKey")] [JsonProperty("paymobPaymentKey")] public string? PaymobPaymentKey { get; set; }
    }
}
