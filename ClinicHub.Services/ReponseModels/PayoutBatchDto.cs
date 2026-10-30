namespace ClinicHub.Services.ReponseModels
{
    public class PayoutBatchDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public int Status { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemsCount { get; set; }
        public decimal SuccessAmount { get; set; }
        public decimal FailedAmount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }
        public decimal? BudgetBefore { get; set; }
        public decimal? BudgetAfter { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? FailureNotes { get; set; }
        public string Currency { get; set; } = "EGP";
        public List<PayoutItemDto> Items { get; set; } = new();
    }

    public class PayoutItemDto
    {
        public Guid Id { get; set; }
        public Guid BatchId { get; set; }
        public string BatchCode { get; set; } = "";
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public int Status { get; set; }
        public string BankCode { get; set; } = "";
        public string MaskedAccountNumber { get; set; } = "";
        public string HolderName { get; set; } = "";
        public string? PaymobTransactionId { get; set; }
        public string? PaymobStatusCode { get; set; }
        public string? DisbursementStatus { get; set; }
        public string? LastError { get; set; }
        public int Attempts { get; set; }
        public bool CanRetry { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? SettledAt { get; set; }
    }

    public class PayoutBudgetDto
    {
        public bool Success { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "EGP";
        public string? StatusCode { get; set; }
        public string? StatusDescription { get; set; }
    }
}
