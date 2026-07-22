namespace ClinicHub.Services.RequestModels
{
    public class UpdateClinicRequest
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? NameAr { get; set; }
        public string? Description { get; set; }
        public string? ArDescription { get; set; }
        public string? Address { get; set; }
        public string? AddressAr { get; set; }
        public string? Phone { get; set; }
        public string Email { get; set; } = null!;
        public string? Website { get; set; }
        public string? Logo { get; set; }
        public string? WorkingHours { get; set; }
        public Guid SpecializationId { get; set; }
        public TimeOnly? WorkingHoursStart { get; set; }
        public TimeOnly? WorkingHoursEnd { get; set; }
        public List<DayOfWeek>? WorkingDays { get; set; }
    }
}
