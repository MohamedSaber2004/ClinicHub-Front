namespace ClinicHub.Services.RequestModels
{
    public class InitiatePaymentRequest
    {
        public Guid PlanId { get; set; }
        public int Period { get; set; }
    }
}
