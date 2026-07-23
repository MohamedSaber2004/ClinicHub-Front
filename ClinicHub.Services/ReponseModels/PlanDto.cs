namespace ClinicHub.Services.ReponseModels
{
    public class PlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string? Description { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal PriceMonthly { get; set; }
        public decimal PriceYearly { get; set; }
        public int? MaxDoctors { get; set; }
        public int? MaxStaff { get; set; }
        public string Features { get; set; } = "[]";
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }
}
