using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    public class StaffAppointmentDto
    {
        [JsonPropertyName("id")] [JsonProperty("id")] public string Id { get; set; } = null!;
        [JsonPropertyName("patient")] [JsonProperty("patient")] public StaffPatientDto Patient { get; set; } = null!;
        [JsonPropertyName("doctor")] [JsonProperty("doctor")] public StaffDoctorBriefDto Doctor { get; set; } = null!;
        [JsonPropertyName("specialty")] [JsonProperty("specialty")] public string Specialty { get; set; } = null!;
        [JsonPropertyName("date")] [JsonProperty("date")] public string Date { get; set; } = null!;
        [JsonPropertyName("time")] [JsonProperty("time")] public string Time { get; set; } = null!;
        [JsonPropertyName("status")] [JsonProperty("status")] public string Status { get; set; } = null!;
        [JsonPropertyName("statusLabel")] [JsonProperty("statusLabel")] public string StatusLabel { get; set; } = null!;
        [JsonPropertyName("statusClass")] [JsonProperty("statusClass")] public string StatusClass { get; set; } = null!;
        [JsonPropertyName("phone")] [JsonProperty("phone")] public string Phone { get; set; } = null!;
    }
}
