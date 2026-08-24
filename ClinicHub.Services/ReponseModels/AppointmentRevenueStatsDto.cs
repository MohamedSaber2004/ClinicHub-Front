namespace ClinicHub.Services.ReponseModels
{
    public class AppointmentRevenueStatsDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal PaidTotal { get; set; }
        public decimal PendingTotal { get; set; }
    }
}
