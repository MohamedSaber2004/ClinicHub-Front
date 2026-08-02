namespace ClinicHub.Services.RequestModels
{
    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? Gender { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
