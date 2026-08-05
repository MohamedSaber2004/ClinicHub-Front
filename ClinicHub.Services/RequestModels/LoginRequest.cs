namespace ClinicHub.Services.RequestModels
{
    using ClinicHub.Services.Enums;

    public record LoginRequest(string Email, string Password, string? FcmToken = null, DevicePlatform? DevicePlatform = null);
}
