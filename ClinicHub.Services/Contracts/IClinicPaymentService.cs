using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Services.Contracts
{
    public interface IClinicPaymentService
    {
        Task<PagginatedResult<AppointmentPaymentDto>> GetAppointmentPaymentsAsync(int? status = null, int? method = null, int pageNumber = 1, int pageSize = 10);
        Task<AppointmentRevenueStatsDto> GetAppointmentRevenueStatsAsync();
    }
}
