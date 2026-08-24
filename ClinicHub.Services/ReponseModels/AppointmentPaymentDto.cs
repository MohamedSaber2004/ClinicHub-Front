namespace ClinicHub.Services.ReponseModels
{
    public class AppointmentPaymentDto
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public int Method { get; set; }
        public int Status { get; set; }
    }
}
