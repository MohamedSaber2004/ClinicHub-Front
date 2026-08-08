using ClinicHub.Services.Enums;

namespace ClinicHub.Services.RequestModels
{
    public record FcmTokenRequest(string FcmToken, DevicePlatform? DevicePlatform = null);
}
