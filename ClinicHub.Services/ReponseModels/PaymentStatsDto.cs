namespace ClinicHub.Services.ReponseModels
{
    public class PaymentStatsDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal SubscriptionsRevenue { get; set; }
        public decimal AppointmentsRevenue { get; set; }
        public decimal AdsRevenue { get; set; }
        public int PendingCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int RefundedCount { get; set; }
    }
}
