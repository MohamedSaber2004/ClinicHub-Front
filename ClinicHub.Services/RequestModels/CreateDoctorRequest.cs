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
        public Guid ClinicId { get; set; }
        public Guid UserId { get; set; }
        public Guid SpecializationId { get; set; }
        public string? Bio { get; set; }
        public int YearsOfExperience { get; set; }
        public List<DoctorAvailabilityItem>? Availabilities { get; set; }
    }
}