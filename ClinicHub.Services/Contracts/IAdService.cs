using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IAdService
    {
        Task<List<AdDto>> GetMyAdsAsync(Guid clinicId, int? status = null);
        Task<List<AdPackageDto>> GetPackagesAsync();
        Task<AdsOrderResponseDto> CreateOrderAsync(Guid clinicId, CreateAdsOrderRequest request);

        Task<PagginatedResult<AdDto>> GetAdsAsync(int pageNumber = 1, int pageSize = 20, int? status = null);
        Task<bool> DeactivateAdAsync(Guid id, string? reason = null);
        Task<List<AdPackageDto>> GetAllPackagesAsync();
        Task<AdPackageDto> CreatePackageAsync(UpsertAdPackageRequest request);
        Task<AdPackageDto> UpdatePackageAsync(Guid id, UpsertAdPackageRequest request);
        Task<bool> DeletePackageAsync(Guid id);

        Task<List<ClinicAdSettingsDto>> GetClinicAdSettingsAsync();
        Task<ClinicAdSettingsDto> UpdateClinicAdSettingsAsync(Guid clinicId, UpdateClinicAdSettingsRequest request);
    }
}
