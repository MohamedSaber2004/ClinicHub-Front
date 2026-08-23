namespace ClinicHub.Services.ReponseModels
{
    public class RevenueTrendPointDto
    {
        public string Period { get; set; } = "";
        public decimal Revenue { get; set; }
        public int PaymentsCount { get; set; }
    }

    public class ClinicsGrowthPointDto
    {
        public string Period { get; set; } = "";
        public int NewClinics { get; set; }
        public int TotalClinics { get; set; }
    }

    public class SubscriptionsByPlanDto
    {
        public Guid PlanId { get; set; }
        public string PlanName { get; set; } = "";
        public int SubscriptionsCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class UsersGrowthPointDto
    {
        public string Period { get; set; } = "";
        public int NewUsers { get; set; }
        public int TotalUsers { get; set; }
    }

    public class AppointmentsSummaryPointDto
    {
        public string Period { get; set; } = "";
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int PendingCount { get; set; }
    }
}
