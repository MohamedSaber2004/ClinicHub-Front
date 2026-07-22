namespace ClinicHub.Services.ReponseModels
{
    public class SpecializationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string ArName { get; set; } = null!;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public bool IsFamous { get; set; }
        public bool IsActive { get; set; }
    }
}
