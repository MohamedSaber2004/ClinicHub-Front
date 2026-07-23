namespace ClinicHub.Services.ReponseModels
{
    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = null!;
        public Guid PlanId { get; set; }
        public string PlanName { get; set; } = null!;
        public int Period { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
        public bool IsActive { get; set; }
    }
}
