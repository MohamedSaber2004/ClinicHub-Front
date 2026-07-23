namespace ClinicHub.Services.RequestModels
{
    public class GetPaginatedSubscriptionsRequest
    {
        public int? Status { get; set; }
        public Guid? PlanId { get; set; }
        public Guid? ClinicId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
