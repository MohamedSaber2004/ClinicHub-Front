namespace ClinicHub.Services.ReponseModels
{
    public class RegisterClinicResponseDto
    {
        public Guid? UserId { get; set; }
        public string? Message { get; set; }
        public bool IsPendingApproval { get; set; }
        public AuthResponseDto? AuthData { get; set; }
        public SignupResponseDto? PendingData { get; set; }
    }

    public record SignupResponseDto(
        Guid UserId,
        string Message,
        bool IsPendingApproval = true);
}
