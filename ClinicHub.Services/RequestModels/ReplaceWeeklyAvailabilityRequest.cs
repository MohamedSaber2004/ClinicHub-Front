using System.Text.Json.Serialization;

namespace ClinicHub.Services.RequestModels
{
    public class DoctorAvailabilityWeekItem : DoctorAvailabilityItem
    {
        [JsonPropertyName("id")]
        public Guid? Id { get; set; }
    }

    public class ReplaceWeeklyAvailabilityRequest
    {
        [JsonPropertyName("days")]
        public List<DoctorAvailabilityWeekItem>? Days { get; set; }
    }
}
