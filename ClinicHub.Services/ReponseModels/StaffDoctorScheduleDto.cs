using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    public class StaffDoctorScheduleDto
    {
        [JsonPropertyName("doctor")] [JsonProperty("doctor")] public StaffDoctorBriefDto Doctor { get; set; } = null!;
        [JsonPropertyName("date")] [JsonProperty("date")] public string Date { get; set; } = null!;
        [JsonPropertyName("appointments")] [JsonProperty("appointments")] public List<StaffScheduleAppointmentDto> Appointments { get; set; } = new();
    }

    public class StaffScheduleAppointmentDto
    {
        [JsonPropertyName("patient")] [JsonProperty("patient")] public StaffPatientDto Patient { get; set; } = null!;
        [JsonPropertyName("time")] [JsonProperty("time")] public string Time { get; set; } = null!;
        [JsonPropertyName("status")] [JsonProperty("status")] public string Status { get; set; } = null!;
        [JsonPropertyName("statusLabel")] [JsonProperty("statusLabel")] public string StatusLabel { get; set; } = null!;
        [JsonPropertyName("statusClass")] [JsonProperty("statusClass")] public string StatusClass { get; set; } = null!;
    }
}
