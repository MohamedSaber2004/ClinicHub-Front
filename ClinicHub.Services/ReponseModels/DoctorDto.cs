using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.ReponseModels
{
    public class DoctorDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string? UserPhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = null!;
        public Guid SpecializationId { get; set; }
        public string SpecializationName { get; set; } = null!;
        public string? Bio { get; set; }
        public int YearsOfExperience { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<DoctorAvailabilityItem>? Availabilities { get; set; }
    }
}