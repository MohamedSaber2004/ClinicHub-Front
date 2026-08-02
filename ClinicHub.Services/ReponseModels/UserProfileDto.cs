namespace ClinicHub.Services.ReponseModels
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int? Gender { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public DateTime? BirthDate { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Language { get; set; } = null!;
        public string? Role { get; set; }
        public bool IsFreelanceDoctor { get; set; }
    }
}
