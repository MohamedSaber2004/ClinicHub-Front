using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    public class DoctorBriefDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Image { get; set; }
        public string? SpecializationArName { get; set; }
        public string? SpecializationEnName { get; set; }
        public string? Bio { get; set; }
        public int YearsOfExperience { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
