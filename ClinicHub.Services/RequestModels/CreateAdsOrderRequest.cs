namespace ClinicHub.Services.RequestModels
{
    public class CreateAdsOrderRequest
    {
        public Guid ClinicId { get; set; }
        public Guid AdPackageId { get; set; }
        public int DurationDays { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
