namespace ClinicHub.Services.RequestModels
{
    public class CreateManualPaymentRequest
    {
        public Guid PayerId { get; set; }
        public int Type { get; set; }
        public decimal Amount { get; set; }
        public int Method { get; set; }
        public string? RefNumber { get; set; }
        public string? Notes { get; set; }
    }
}
