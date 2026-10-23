namespace ClinicHub.Services.RequestModels
{
    public class RevokeSubscriptionRequest
    {
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Optional refund override. Null = use the server-side pro-rated quote;
        /// 0 = cancel with no refund (cash/manual payments).
        /// </summary>
        public decimal? RefundAmount { get; set; }

        /// <summary>Optional human-readable cancellation reason.</summary>
        public string? Reason { get; set; }
    }
}
