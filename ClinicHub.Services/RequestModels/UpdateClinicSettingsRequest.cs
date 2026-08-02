namespace ClinicHub.Services.RequestModels
{
    public class UpdateClinicSettingsRequest
    {
        public string Name { get; set; } = null!;
        public string? ResponsibleDoctor { get; set; }
        public string? Description { get; set; }
        public string? Phone { get; set; }
        public string? ManagerName { get; set; }
        public string? Location { get; set; }
        public Guid SpecializationId { get; set; }
        public decimal ConsultationFee { get; set; }
        public string? Currency { get; set; }
        public int MaxAdvanceBookingDays { get; set; }
        public int ReservationTtlMinutes { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
