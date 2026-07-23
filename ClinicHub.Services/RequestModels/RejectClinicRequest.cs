namespace ClinicHub.Services.RequestModels
{
    public class RejectClinicRequest
    {
        public Guid ClinicId { get; set; }
        public string? Reason { get; set; }
    }
}
