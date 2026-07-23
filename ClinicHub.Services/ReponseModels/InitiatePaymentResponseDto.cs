namespace ClinicHub.Services.ReponseModels
{
    public class InitiatePaymentResponseDto
    {
        public Guid PaymentId { get; set; }
        public string PaymobRedirectUrl { get; set; } = null!;
        public string PaymobPaymentKey { get; set; } = null!;
        public Guid PlanId { get; set; }
        public string PlanName { get; set; } = null!;
        public int Period { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
    }
}
