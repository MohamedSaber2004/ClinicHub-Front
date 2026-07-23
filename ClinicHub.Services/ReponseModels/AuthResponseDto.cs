namespace ClinicHub.Services.ReponseModels
{
    public record AuthResponseDto(
        string? AccessToken,
        string? RefreshToken,
        string FullName,
        string Email,
        string Roles,
        Guid Id,
        Guid? ClinicId = null,
        string? ProfilePictureUrl = null,
        bool IsFreelanceDoctor = false);
}
