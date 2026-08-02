namespace ClinicHub.Services.ReponseModels
{
    public class ClinicBookingDto
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = null!;
        public string PatientPhone { get; set; } = null!;
        public int PatientAge { get; set; }
        public string ClinicName { get; set; } = null!;
        public string DoctorName { get; set; } = null!;
        public string RequestedDate { get; set; } = null!;
        public string RequestedTime { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public string AppointmentType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
