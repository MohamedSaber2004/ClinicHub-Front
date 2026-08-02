namespace ClinicHub.Services.ReponseModels
{
    public class AdminPaymentDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public int Type { get; set; }
        public string Payer { get; set; } = "";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public int Method { get; set; }
        public int Status { get; set; }
        public DateTime Date { get; set; }
        public string RefNumber { get; set; } = "";
    }
}
