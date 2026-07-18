namespace ClinicHub.Services.RequestModels
{
    public class ChangePasswordRequest
    {
        public Guid Id { get; set; }
        public string? OldPassword { get; set; }
        public string ConfirmPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
