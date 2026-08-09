using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Services.Contracts
{
    public interface IRatingsService
    {
        Task<List<RatingDto>> GetDoctorRatingsAsync(Guid doctorId);
        Task<List<RatingDto>> GetClinicRatingsAsync(Guid clinicId);
        Task<List<RatingDto>> GetPlaceCleanlinessRatingsAsync(Guid clinicId);
        Task<List<RatingDto>> GetReceptionRatingsAsync(Guid clinicId);
    }
}
