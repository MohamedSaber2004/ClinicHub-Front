using ClinicHub.Services.Enums;

namespace ClinicHub.Services.ReponseModels
{
    public class UserVerficationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string UserPhoneNumber { get; set; } = null!;
        public VerificationStatus Status { get; set; }
        public UserType RequestedRole { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedByFullName { get; set; }
        public string? Notes { get; set; }
        public string? ProfessionalPracticeCardImage { get; set; }
        public string? TaxCardImage { get; set; }
        public string? UnionIdCardImage { get; set; }
        public string? DoctorImage { get; set; }
    }
}
