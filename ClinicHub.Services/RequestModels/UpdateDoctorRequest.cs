using System.Text.Json.Serialization;

namespace ClinicHub.Services.RequestModels
{
    public class UpdateDoctorRequest
    {
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("birthDate")]
        public string? BirthDate { get; set; }

        [JsonPropertyName("gender")]
        public int? Gender { get; set; }

        [JsonPropertyName("doctorImage")]
        public string? DoctorImage { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        [JsonPropertyName("yearsOfExperience")]
        public int? YearsOfExperience { get; set; }

        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("availabilities")]
        public List<DoctorAvailabilityItem>? Availabilities { get; set; }
    }
}