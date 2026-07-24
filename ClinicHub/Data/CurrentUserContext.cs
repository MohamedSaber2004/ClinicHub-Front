namespace ClinicHub.Data
{
    public class CurrentUserContext
    {
        public int Id { get; set; }
        public Guid? ClinicId { get; set; }
        public UserRole Role { get; set; }
        public Permission Permissions { get; set; }
        public PlanFeature PlanFeatures { get; set; }
        public string? PlanId { get; set; }
        public string? PlanName { get; set; }
        public int? MaxDoctors { get; set; }
        public int? MaxStaff { get; set; }
        public bool HasActivePlan { get; set; } = true;

        public bool Has(Permission permission) => (Permissions & permission) == permission;
        public bool HasFeature(PlanFeature feature) => (PlanFeatures & feature) == feature;
    }
}
