namespace ClinicHub.Services.ReponseModels
{
    public class PayoutPreviewDto
    {
        public decimal MinimumAmount { get; set; }
        public decimal TotalEligible { get; set; }
        public List<PayoutPreviewItemDto> Items { get; set; } = new();
    }

    public class PayoutPreviewItemDto
    {
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = "";
        public decimal AvailableBalance { get; set; }
        public string Currency { get; set; } = "EGP";
        public bool Eligible { get; set; }
        public string? IneligibilityReason { get; set; }
        public int? AccountStatus { get; set; }
        public string? AccountLast4 { get; set; }
    }
}
