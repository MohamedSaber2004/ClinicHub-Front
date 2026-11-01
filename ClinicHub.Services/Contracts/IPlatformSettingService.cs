using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Services.Contracts
{
    public interface IPlatformSettingService
    {
        Task<PlatformSettingDto> GetSettingAsync();
        Task<PlatformSettingDto> UpdateSettingAsync(decimal appointmentFeePercent);
        Task<PlatformSettingDto> UpdatePayoutMinimumAsync(decimal minimumPayoutAmount);
    }
}
