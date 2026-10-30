namespace ClinicHub.Services.RequestModels
{
    public class CreatePayoutBatchRequest
    {
        public decimal? MinimumAmount { get; set; }
        public List<Guid>? ClinicIds { get; set; }
    }

    public class PayoutBatchActionRequest
    {
        public Guid Id { get; set; }
    }
}
