namespace ClinicHub.Services.ReponseModels
{
    public class ClinicSettingsDto
    {
        public string Name { get; set; } = null!;
        public string? ResponsibleDoctor { get; set; }
        public string? Description { get; set; }
        public string? Phone { get; set; }
        public string? ManagerName { get; set; }
        public string? Location { get; set; }
        public Guid SpecializationId { get; set; }
        public string? SpecializationName { get; set; }
        public string? SpecializationNameAr { get; set; }
        public double Latitude { get; set; } = 31.0409;
        public double Longitude { get; set; } = 31.3785;
        public bool IsActive { get; set; } = true;
        public decimal ConsultationFee { get; set; }
        public string Currency { get; set; } = "EGP";
        public int MaxAdvanceBookingDays { get; set; } = 30;
        public int ReservationTtlMinutes { get; set; } = 10;
        public int CancellationWindowMinutes { get; set; } = 120;
        public int SlotDurationMinutes { get; set; } = 30;
    }
}
