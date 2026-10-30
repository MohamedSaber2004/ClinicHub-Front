namespace ClinicHub.Services.ReponseModels
{
    public class ClinicPayoutSummaryDto
    {
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal ReservedBalance { get; set; }
        public decimal PaidTotal { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal MinimumPayoutAmount { get; set; }
        public int? AccountStatus { get; set; }
        public bool IsPayoutEnabled { get; set; }
    }
}
