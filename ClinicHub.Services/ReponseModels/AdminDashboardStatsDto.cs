namespace ClinicHub.Services.ReponseModels
{
    public class AdminDashboardStatsDto
    {
        public int VerificationRequestsCount { get; set; }
        public int ActiveClinicsCount { get; set; }
        public int TotalUsersCount { get; set; }
        public int SpecializationsCount { get; set; }
        public int ActiveAdsCount { get; set; }
        public int RevokedSubscriptionsCount { get; set; }
    }
}
