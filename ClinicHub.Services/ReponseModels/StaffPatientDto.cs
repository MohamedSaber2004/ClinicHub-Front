using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    public class StaffPatientDto
    {
        [JsonPropertyName("id")] [JsonProperty("id")] public string Id { get; set; } = null!;
        [JsonPropertyName("name")] [JsonProperty("name")] public string Name { get; set; } = null!;
        [JsonPropertyName("initial")] [JsonProperty("initial")] public string Initial { get; set; } = null!;
    }
}
