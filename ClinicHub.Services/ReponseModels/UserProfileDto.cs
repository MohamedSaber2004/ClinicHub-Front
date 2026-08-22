using Newtonsoft.Json;

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

        // The backend serializes this field as "roles" (see AuthDto.UserProfileDto record);
        // without this mapping the property stayed null and every role-based check failed.
        [JsonProperty("roles")]
        public string? Role { get; set; }

        public bool IsFreelanceDoctor { get; set; }
    }
}
