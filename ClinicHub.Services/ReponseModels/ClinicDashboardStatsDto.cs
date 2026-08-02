namespace ClinicHub.Services.ReponseModels
{
    public class ClinicDashboardStatsDto
    {
        public int TodayVisits { get; set; }
        public double TodayIncome { get; set; }
        public int WeeklyVisits { get; set; }
        public double WeeklyIncome { get; set; }
        public int MonthlyVisits { get; set; }
        public double MonthlyIncome { get; set; }
        public int YearlyVisits { get; set; }
        public double YearlyIncome { get; set; }
        public int PendingActions { get; set; }
    }
}
