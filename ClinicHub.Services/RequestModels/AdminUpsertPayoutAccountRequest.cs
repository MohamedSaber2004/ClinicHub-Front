namespace ClinicHub.Services.RequestModels
{
    public class AdminUpsertPayoutAccountRequest
    {
        public Guid ClinicId { get; set; }
        public string HolderName { get; set; } = "";
        public string NationalId { get; set; } = "";
        public string BankCode { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string? BankTransactionType { get; set; }
    }
}
