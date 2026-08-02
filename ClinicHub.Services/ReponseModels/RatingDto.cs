using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    public class RatingDto
    {
        public string Id { get; set; } = null!;
        public string? UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string? DoctorId { get; set; }
        public string? ClinicId { get; set; }
        public int Value { get; set; }
        public string? Review { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
