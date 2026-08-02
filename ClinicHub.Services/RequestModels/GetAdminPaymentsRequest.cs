namespace ClinicHub.Services.RequestModels
{
    public class GetAdminPaymentsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int? Type { get; set; }
        public int? Status { get; set; }
        public int? Method { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
    }
}
