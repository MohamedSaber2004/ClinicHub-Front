namespace ClinicHub.Services.ReponseModels
{
    public class AdPackageDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string NameAr { get; set; } = "";
        public string Description { get; set; } = "";
        public string DescriptionAr { get; set; } = "";
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; }
    }
}
