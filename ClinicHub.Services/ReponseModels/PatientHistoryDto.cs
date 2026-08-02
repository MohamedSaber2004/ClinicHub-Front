namespace ClinicHub.Services.ReponseModels
{
    public class PatientHistoryDto
    {
        public Guid AppointmentId { get; set; }
        public string AppointmentDate { get; set; } = null!;
        public string StartTime { get; set; } = null!;
        public string EndTime { get; set; } = null!;
        public int AppointmentType { get; set; }
        public int Status { get; set; }
        public string Complaint { get; set; } = null!;
        public string? ChronicDiseases { get; set; }
        public string? CancellationReason { get; set; }
    }
}
