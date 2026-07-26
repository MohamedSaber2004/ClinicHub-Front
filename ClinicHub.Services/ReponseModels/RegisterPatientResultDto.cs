using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    public class RegisterPatientResultDto
    {
        [JsonPropertyName("appointmentId")] [JsonProperty("appointmentId")] public string AppointmentId { get; set; } = null!;
        [JsonPropertyName("patientId")] [JsonProperty("patientId")] public string PatientId { get; set; } = null!;
        [JsonPropertyName("queueNumber")] [JsonProperty("queueNumber")] public int QueueNumber { get; set; }
        [JsonPropertyName("message")] [JsonProperty("message")] public string Message { get; set; } = null!;
    }
}
