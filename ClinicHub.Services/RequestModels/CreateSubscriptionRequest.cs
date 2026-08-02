namespace ClinicHub.Services.RequestModels
{
    public class CreateSubscriptionRequest
    {
        public Guid ClinicId { get; set; }
        public Guid PlanId { get; set; }
        public int Period { get; set; }
        public DateTime? StartDate { get; set; }
        public decimal? Amount { get; set; }
    }
}
