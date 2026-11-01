using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IDoctorDashboardService
    {
        Task<DoctorDashboardStatsDto> GetStatsAsync();
        Task<List<DoctorAppointmentDto>?> GetRecentAppointmentsAsync(int limit = 5);
        Task<PagginatedResult<DoctorAppointmentDto>> GetAppointmentsAsync(int? status = null, string? searchTerm = null, string? startDate = null, string? endDate = null, int pageNumber = 1, int pageSize = 10);
        Task<AppointmentAcceptResponseDto?> AcceptAppointmentAsync(Guid id);
        Task<bool> RejectAppointmentAsync(Guid id, string? reason = null);
        Task<bool> CompleteAppointmentAsync(Guid id);
        Task<AppointmentAcceptResponseDto?> UpdateStatusAsync(Guid id, int status, string? notes = null);
        Task<PagginatedResult<DoctorPatientDto>> GetPatientsAsync(string? searchTerm = null, int pageNumber = 1, int pageSize = 10);
        Task<RegisterPatientResultDto> RegisterPatientAsync(RegisterPatientFromStaffRequest request);
        Task<PagginatedResult<PatientHistoryDto>> GetPatientHistoryAsync(Guid patientId, int pageNumber = 1, int pageSize = 10);
        Task<PatientAssessmentDto?> GetPatientAssessmentAsync(Guid patientId);
        Task<bool> SavePatientAssessmentAsync(Guid patientId, PatientAssessmentDto assessment);
    }
}
