using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    public class StaffDoctorBriefDto
    {
        [JsonPropertyName("id")] [JsonProperty("id")] public string Id { get; set; } = null!;
        [JsonPropertyName("name")] [JsonProperty("name")] public string Name { get; set; } = null!;
        [JsonPropertyName("specialty")] [JsonProperty("specialty")] public string Specialty { get; set; } = null!;
    }
}
