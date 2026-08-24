using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Services.Contracts
{
        public interface IAdminDashboardService
        {
            Task<AdminDashboardStatsDto> GetStatsAsync();
            Task<AdminUserOverviewDto> GetUserOverviewAsync(Guid userId);
            Task<List<RevenueTrendPointDto>> GetRevenueTrendAsync(string granularity = "day", DateTime? fromDate = null, DateTime? toDate = null);
            Task<List<ClinicsGrowthPointDto>> GetClinicsGrowthAsync(string granularity = "day", DateTime? fromDate = null, DateTime? toDate = null);
            Task<List<SubscriptionsByPlanDto>> GetSubscriptionsByPlanAsync(DateTime? fromDate = null, DateTime? toDate = null);
            Task<List<UsersGrowthPointDto>> GetUsersGrowthAsync(string granularity = "day", DateTime? fromDate = null, DateTime? toDate = null);
            Task<List<AppointmentsSummaryPointDto>> GetAppointmentsSummaryAsync(string granularity = "day", DateTime? fromDate = null, DateTime? toDate = null);
        }
}
