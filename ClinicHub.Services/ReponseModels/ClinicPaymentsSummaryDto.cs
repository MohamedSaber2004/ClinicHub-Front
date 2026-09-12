namespace ClinicHub.Services.ReponseModels
{
    public class ClinicPaymentsSummaryDto
    {
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = "";
        public int PaymentsCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PlatformFees { get; set; }
        public decimal NetRevenue { get; set; }
    }
}
