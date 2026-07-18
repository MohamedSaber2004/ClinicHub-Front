using ClinicHub.Services.Enums;

namespace ClinicHub.Services.RequestModels
{
    public class EditUserRequest
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; } = null!;
        public DateTime? BirthDate { get; set; }
        public Gender? Gender { get; set; }
        public bool? IsActive { get; set; }
    }
}
