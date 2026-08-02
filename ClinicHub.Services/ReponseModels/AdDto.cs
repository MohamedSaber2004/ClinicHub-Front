namespace ClinicHub.Services.ReponseModels
{
    public class AdDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = "";
        public Guid PackageId { get; set; }
        public string PackageNameAr { get; set; } = "";
        public int DurationDays { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public int Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
