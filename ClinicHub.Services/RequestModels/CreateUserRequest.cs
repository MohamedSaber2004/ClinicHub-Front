using ClinicHub.Services.Enums;

namespace ClinicHub.Services.RequestModels
{
    public class CreateUserRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public DateTime BirthDate { get; set; }
        public Gender Gender { get; set; }
        public UserType Role { get; set; }
        public Guid? ClinicId { get; set; }
        public Guid? SpecializationId { get; set; }
        public string? Bio { get; set; }
        public int? YearsOfExperience { get; set; }
    }
}
