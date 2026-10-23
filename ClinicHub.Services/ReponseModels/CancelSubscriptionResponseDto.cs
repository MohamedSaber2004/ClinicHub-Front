namespace ClinicHub.Services.ReponseModels
{
    /// <summary>
    /// Result of a successful cancellation (owner or admin). The cancellation always
    /// succeeds once this is returned; <see cref="RefundStatus"/> only describes the money.
    /// Enums arrive as numbers: RefundStatus 0=None 1=Pending 2=Processing 3=Completed 4=Failed 5=NeedsReview.
    /// </summary>
    public class CancelSubscriptionResponseDto
    {
        public Guid SubscriptionId { get; set; }
        public DateTime CancelledAt { get; set; }
        public int TotalDays { get; set; }
        public int UsedDays { get; set; }
        public decimal ChargedAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public string Currency { get; set; } = "EGP";
        public int RefundStatus { get; set; }
        public string? RefundId { get; set; }
    }
}
