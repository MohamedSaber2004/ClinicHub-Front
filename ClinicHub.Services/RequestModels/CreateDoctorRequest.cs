using System.Text.Json.Serialization;

namespace ClinicHub.Services.RequestModels
{
    public class DoctorAvailabilityItem
    {
        [JsonPropertyName("dayOfWeek")]
        public int DayOfWeek { get; set; }

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = null!;

        [JsonPropertyName("endTime")]
        public string EndTime { get; set; } = null!;

        [JsonPropertyName("slotDurationMinutes")]
        public int SlotDurationMinutes { get; set; } = 30;
    }

    public class CreateDoctorRequest
    {
        [JsonPropertyName("clinicId")]
        public Guid ClinicId { get; set; }

        [JsonPropertyName("specializationId")]
        public Guid SpecializationId { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = null!;

        [JsonPropertyName("email")]
        public string Email { get; set; } = null!;

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; } = null!;

        [JsonPropertyName("password")]
        public string Password { get; set; } = null!;

        [JsonPropertyName("gender")]
        public int Gender { get; set; }

        [JsonPropertyName("birthDate")]
        public string? BirthDate { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        [JsonPropertyName("yearsOfExperience")]
        public int YearsOfExperience { get; set; }

        [JsonPropertyName("doctorImage")]
        public string? DoctorImage { get; set; }

        [JsonPropertyName("availabilities")]
        public List<DoctorAvailabilityItem>? Availabilities { get; set; }
    }
}