using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IDoctorService
    {
        Task<PagginatedResult<UserResponseDto>> GetAllDoctorsPagginatedAsync(GetAllDoctorsRequest request);

        Task<DoctorDto> GetMyProfileAsync();

        Task<List<DoctorAvailabilityDto>> GetMyAvailabilityAsync();

        Task<List<DoctorAvailabilityDto>> ReplaceWeeklyAvailabilityAsync(ReplaceWeeklyAvailabilityRequest request);
    }
}
