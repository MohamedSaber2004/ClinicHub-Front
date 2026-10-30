namespace ClinicHub.Services.RequestModels
{
    public class PayoutAccountActionRequest
    {
        public Guid Id { get; set; }
    }

    public class RejectPayoutAccountRequest
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = "";
    }
}
