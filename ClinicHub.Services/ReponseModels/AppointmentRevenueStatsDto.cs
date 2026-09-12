namespace ClinicHub.Services.ReponseModels
{
    public class AppointmentRevenueStatsDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal PaidTotal { get; set; }
        public decimal PendingTotal { get; set; }

        // Net breakdown (clinic share only, without admin platform fees).
        // Nullable-tolerant: when the backend stats endpoint returns only totals,
        // the view falls back to the totals above so nothing renders as zero.
        public decimal? TodayPlatformFees { get; set; }
        public decimal? TodayNetRevenue { get; set; }
        public decimal? MonthPlatformFees { get; set; }
        public decimal? MonthNetRevenue { get; set; }
        public decimal? PaidPlatformFees { get; set; }
        public decimal? PaidNetTotal { get; set; }
        public decimal? PendingPlatformFees { get; set; }
        public decimal? PendingNetTotal { get; set; }

        public decimal TodayNet => TodayNetRevenue ?? TodayRevenue;
        public decimal MonthNet => MonthNetRevenue ?? MonthRevenue;
        public decimal PaidNet => PaidNetTotal ?? PaidTotal;
        public decimal PendingNet => PendingNetTotal ?? PendingTotal;
    }
}
