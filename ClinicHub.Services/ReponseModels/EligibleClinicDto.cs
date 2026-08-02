namespace ClinicHub.Services.ReponseModels
{
    public class EligibleClinicDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }
}
