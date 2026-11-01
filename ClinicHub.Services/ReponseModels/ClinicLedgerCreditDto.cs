namespace ClinicHub.Services.ReponseModels
{
    public class ClinicLedgerCreditDto
    {
        public Guid EntryId { get; set; }
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal AvailableBalance { get; set; }
    }
}
