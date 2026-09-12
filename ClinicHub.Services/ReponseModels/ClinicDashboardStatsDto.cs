namespace ClinicHub.Services.ReponseModels
{
    public class ClinicDashboardStatsDto
    {
        public int TodayVisits { get; set; }
        public decimal TodayIncome { get; set; }
        public int WeeklyVisits { get; set; }
        public decimal WeeklyIncome { get; set; }
        public int MonthlyVisits { get; set; }
        public decimal MonthlyIncome { get; set; }
        public int YearlyVisits { get; set; }
        public decimal YearlyIncome { get; set; }
        public decimal TodayPlatformFees { get; set; }
        public decimal TodayNetIncome { get; set; }
        public decimal WeeklyPlatformFees { get; set; }
        public decimal WeeklyNetIncome { get; set; }
        public decimal MonthlyPlatformFees { get; set; }
        public decimal MonthlyNetIncome { get; set; }
        public decimal YearlyPlatformFees { get; set; }
        public decimal YearlyNetIncome { get; set; }
        public int PendingActions { get; set; }
    }
}
