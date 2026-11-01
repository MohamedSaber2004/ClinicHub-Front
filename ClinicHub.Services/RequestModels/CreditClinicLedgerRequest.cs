namespace ClinicHub.Services.RequestModels
{
    public class CreditClinicLedgerRequest
    {
        public Guid ClinicId { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }
}
