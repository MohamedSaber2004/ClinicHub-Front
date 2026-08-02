namespace ClinicHub.Services.ReponseModels
{
    public class ClinicDetailsDto : ClinicManagmentDto
    {
        public List<DoctorBriefDto>? Doctors { get; set; }
        public List<StaffBriefDto>? Staff { get; set; }
        public double? AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public List<RatingDto>? RecentRatings { get; set; }
    }
}
