namespace ClinicHub.Services.ReponseModels
{
    /// <summary>
    /// Pro-rated cancellation preview returned by GET /subscriptions/my/cancellation-quote.
    /// <see cref="Reason"/> is localized by the API (it echoes Accept-Language) and is
    /// display-ready when <see cref="Eligible"/> is false.
    /// </summary>
    public class SubscriptionCancellationQuoteDto
    {
        public Guid SubscriptionId { get; set; }
        public bool Eligible { get; set; }
        public string? Reason { get; set; }
        public string? PlanName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public int UsedDays { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChargeAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public string Currency { get; set; } = "EGP";
        public int RefundStatus { get; set; }
    }
}
