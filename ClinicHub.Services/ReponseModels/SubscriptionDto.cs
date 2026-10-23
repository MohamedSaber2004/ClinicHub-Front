namespace ClinicHub.Services.ReponseModels
{
    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = null!;
        public Guid PlanId { get; set; }
        public string PlanName { get; set; } = null!;
        public int Period { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
        public bool IsActive { get; set; }

        // Cancellation + refund state (numbers on the wire).
        // CancellationSource: 0=ClinicOwner 1=Admin 2=System
        // RefundStatus: 0=None 1=Pending 2=Processing 3=Completed 4=Failed 5=NeedsReview
        public DateTime? CancelledAt { get; set; }
        public int? CancellationSource { get; set; }
        public decimal? RefundAmount { get; set; }
        public int? RefundStatus { get; set; }
    }
}
