namespace ClinicHub.Services.ReponseModels
{
    public class DoctorDashboardStatsDto
    {
        public int TodayAppointmentsCount { get; set; }
        public int TotalPatientsCount { get; set; }
        public int PendingAppointmentsCount { get; set; }
        public int CompletedAppointmentsCount { get; set; }
        public int AcceptedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int TotalPatientsThisWeek { get; set; }
        public DoctorNextAppointmentDto? NextAppointment { get; set; }
    }

    public class DoctorNextAppointmentDto
    {
        public string PatientFullName { get; set; } = null!;
        public string PatientPhoneNumber { get; set; } = null!;
        public string AppointmentDate { get; set; } = null!;
        public string StartTime { get; set; } = null!;
        public string EndTime { get; set; } = null!;
        public string Complaint { get; set; } = null!;
    }
}
