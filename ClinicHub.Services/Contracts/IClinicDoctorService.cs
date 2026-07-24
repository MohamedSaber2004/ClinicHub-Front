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
    }
}