namespace ClinicHub.Services.RequestModels
{
    public class RejectUserVerificationRequest
    {
        public Guid UserId { get; set; }
        public string? Notes { get; set; }
    }
}
