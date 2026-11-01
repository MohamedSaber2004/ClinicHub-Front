namespace ClinicHub.Services.ReponseModels
{
    public class PayoutAccountAdminDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = "";
        public string HolderName { get; set; } = "";
        public string BankCode { get; set; } = "";
        public string MaskedAccountNumber { get; set; } = "";
        public string MaskedNationalId { get; set; } = "";
        public string BankTransactionType { get; set; } = "";
        public bool HasAccount { get; set; }
        public int? Status { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal ReservedBalance { get; set; }
    }
}
