namespace ClinicHub.Services.RequestModels
{
    public class RegisterPatientFromStaffRequest
    {
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? Email { get; set; }
        public int? Age { get; set; }
        public int? Gender { get; set; }
        public string DoctorId { get; set; } = null!;
        public string ClinicId { get; set; } = "00000000-0000-0000-0000-000000000000";
        public DateTime AppointmentDate { get; set; }
        public string StartTime { get; set; } = null!;
        public string EndTime { get; set; } = null!;
        public int AppointmentType { get; set; }
        public string? Complaint { get; set; }
        public string? ChronicDiseases { get; set; }
    }
}
