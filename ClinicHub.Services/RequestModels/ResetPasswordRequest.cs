namespace ClinicHub.Services.RequestModels
{
    public record ResetPasswordRequest(
        string Email,
        string Token,
        string NewPassword,
        string ConfirmPassword
    );
}
