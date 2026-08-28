namespace ClinicHub.Services.ReponseModels
{
    public class ClinicAdSettingsDto
    {
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = "";
        public int MaxAds { get; set; }
        public int MaxImpressions { get; set; }
        public int ActiveAdsCount { get; set; }
    }
}
