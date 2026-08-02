using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IClinicDoctorService
    {
        Task<PagginatedResult<DoctorDto>> GetDoctorsAsync(Guid clinicId, int pageNumber = 1, int pageSize = 20, string? searchTerm = null, Guid? specializationId = null);
        Task<DoctorDto?> GetDoctorByIdAsync(Guid id);
        Task<DoctorDto> CreateDoctorAsync(CreateDoctorRequest request);
        Task<DoctorDto> UpdateDoctorAsync(Guid id, UpdateDoctorRequest request);
        Task<bool> DeleteDoctorAsync(Guid id);
        Task<bool> ChangeDoctorPasswordAsync(Guid id, ChangePasswordRequest request);

        /// <summary>Patient booking: fetch generated slots for a doctor on a specific date (slots endpoint).</summary>
        Task<AvailableSlotsDto?> GetAvailableSlotsAsync(Guid clinicId, Guid doctorId, string date);

        /// <summary>Patient booking: create an appointment using exact slot times from the slots endpoint.</summary>
        Task<string> BookAppointmentAsync(BookAppointmentRequest request);
    }
}