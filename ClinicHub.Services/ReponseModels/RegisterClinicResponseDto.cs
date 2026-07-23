namespace ClinicHub.Services.ReponseModels
{
    public class RegisterClinicResponseDto
    {
        public Guid UserId { get; set; }
        public string Message { get; set; } = null!;
        public bool IsPendingApproval { get; set; }
    }
}
