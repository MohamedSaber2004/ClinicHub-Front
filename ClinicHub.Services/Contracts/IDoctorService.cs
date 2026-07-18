using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IDoctorService
    {
        Task<PagginatedResult<UserResponseDto>> GetAllDoctorsPagginatedAsync(GetAllDoctorsRequest request);
    }
}
