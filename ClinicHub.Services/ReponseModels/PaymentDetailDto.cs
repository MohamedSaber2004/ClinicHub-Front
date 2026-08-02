namespace ClinicHub.Services.ReponseModels
{
    public class PaymentTimelineEntryDto
    {
        public DateTime Date { get; set; }
        public string Text { get; set; } = "";
        public string Marker { get; set; } = "info";
    }

    public class PaymentDetailDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public int Type { get; set; }
        public string Payer { get; set; } = "";
        public string PayerType { get; set; } = "";
        public string? PayerEmail { get; set; }
        public string? PayerPhone { get; set; }
        public string ItemName { get; set; } = "";
        public decimal Amount { get; set; }
        public int Method { get; set; }
        public string? TransactionId { get; set; }
        public string RefNumber { get; set; } = "";
        public int Status { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; } = "";
        public List<PaymentTimelineEntryDto> Timeline { get; set; } = new();
    }
}
