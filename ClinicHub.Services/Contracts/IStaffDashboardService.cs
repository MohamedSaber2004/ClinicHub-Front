using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IStaffDashboardService
    {
        Task<StaffDashboardStatsDto> GetStatsAsync();
        Task<PagginatedResult<StaffAppointmentDto>> GetAppointmentsAsync(string? status = null, string? date = null, string? patientName = null, int pageNumber = 1, int pageSize = 10);
        Task<List<StaffQueueItemDto>> GetQueueAsync();
        Task<AppointmentAcceptResponseDto?> ApproveAppointmentAsync(string id);
        Task<bool> RejectAppointmentAsync(string id, string? reason = null);
        Task<bool> CheckInPatientAsync(string id);
        Task<bool> CompleteAppointmentAsync(string id);
        Task<RegisterPatientResultDto> RegisterPatientAsync(RegisterPatientFromStaffRequest request);
        Task<List<StaffDoctorDto>> GetDoctorsAsync();
        Task<StaffDoctorScheduleDto> GetDoctorScheduleAsync(string doctorId, string? date = null);
    }
}
