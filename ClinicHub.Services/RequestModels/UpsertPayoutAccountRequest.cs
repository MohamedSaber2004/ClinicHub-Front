namespace ClinicHub.Services.RequestModels
{
    public class UpsertPayoutAccountRequest
    {
        public string HolderName { get; set; } = "";
        public string NationalId { get; set; } = "";
        public string BankCode { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string? BankTransactionType { get; set; }
    }
}
