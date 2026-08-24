using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Services.Contracts
{
    public interface IClinicDashboardService
    {
        Task<ClinicDashboardStatsDto> GetStatsAsync();
        Task<ClinicOperationalReportDto> GetOperationalReportAsync(string? period = "week", Guid? doctorId = null);
        Task<List<RevenueTrendPointDto>> GetRevenueTrendAsync(string? granularity = "day", DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<AppointmentsSummaryPointDto>> GetAppointmentsSummaryAsync(string? granularity = "day", DateTime? fromDate = null, DateTime? toDate = null);
        Task<PagginatedResult<ClinicBookingDto>> GetBookingsAsync(string? status = null, int pageNumber = 1, int pageSize = 20);
        Task<bool> AcceptBookingAsync(Guid id);
        Task<bool> RejectBookingAsync(Guid id, string? reason = null);
    }
}
