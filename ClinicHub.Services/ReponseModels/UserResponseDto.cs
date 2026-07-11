using ClinicHub.Services.Enums;

namespace ClinicHub.Services.ReponseModels
{
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public DateTime? BirthDate { get; set; }
        public Gender? Gender { get; set; }
        public bool IsActive { get; set; }
        public IList<UserType> Roles { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
