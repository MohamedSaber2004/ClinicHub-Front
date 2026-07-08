namespace ClinicHub.Services.RequestModels
{
    public record VerifyResetTokenRequest(string Token, string Email);
}
