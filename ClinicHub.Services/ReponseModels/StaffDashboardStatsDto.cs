using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    public class StaffDashboardStatsDto
    {
        [JsonPropertyName("totalAppointments")] [JsonProperty("totalAppointments")] public int TotalAppointments { get; set; }
        [JsonPropertyName("checkedIn")] [JsonProperty("checkedIn")] public int CheckedIn { get; set; }
        [JsonPropertyName("waiting")] [JsonProperty("waiting")] public int Waiting { get; set; }
        [JsonPropertyName("completed")] [JsonProperty("completed")] public int Completed { get; set; }
    }
}
