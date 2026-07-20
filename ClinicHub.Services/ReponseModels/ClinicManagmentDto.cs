using ClinicHub.Services.Enums;

namespace ClinicHub.Services.ReponseModels
{
    public class ClinicManagmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? NameAr { get; set; }
        public string? Description { get; set; }
        public string? ArDescription { get; set; }
        public string? Address { get; set; }
        public string? AddressAr { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Logo { get; set; }
        public string? WorkingHours { get; set; }
        public List<WorkingDayDto>? WorkingDays { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public bool IsRegistered { get; set; }
        public ClinicStatus Status { get; set; }
        public bool IsActive { get; set; }
        public Guid SpecializationId { get; set; }
        public string? SpecializationName { get; set; }
        public string? SpecializationNameAr { get; set; }
        public double? Rating { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ClinicAdminId { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerEmail { get; set; }
        public string? OwnerPhone { get; set; }
        public SubscriptionStatus? SubscriptionStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }
    }

    public class WorkingDayDto
    {
        public string DayOfWeek { get; set; } = null!;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}
