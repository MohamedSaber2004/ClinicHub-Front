using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    public class DoctorAvailabilityDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("dayOfWeek")]
        public int DayOfWeek { get; set; }

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = null!;

        [JsonPropertyName("endTime")]
        public string EndTime { get; set; } = null!;

        [JsonPropertyName("slotDurationMinutes")]
        public int SlotDurationMinutes { get; set; }
    }
}
