namespace ClinicHub.Services.ReponseModels
{
    public class ClinicOperationalReportDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedVisits { get; set; }
        public double CompletionRate { get; set; }
        public int CancelledVisits { get; set; }
        public double CancellationRate { get; set; }
        public int NoShowVisits { get; set; }
        public double NoShowRate { get; set; }
        public double PeriodRevenue { get; set; }
        public string PeakTimeSlot { get; set; } = "-";
        public string BusiestDayName { get; set; } = "-";
        public List<OperationalHourSlotDto> HourlyTraffic { get; set; } = new();
        public List<OperationalDayLoadDto> WeeklyWorkload { get; set; } = new();
        public List<OperationalDoctorStatDto> Doctors { get; set; } = new();
        public List<OperationalVisitLogDto> RecentVisits { get; set; } = new();
    }

    public class OperationalHourSlotDto
    {
        public string SlotLabel { get; set; } = "";
        public int AppointmentCount { get; set; }
        public int HeightPercentage { get; set; }
        public bool IsPeak { get; set; }
    }

    public class OperationalDayLoadDto
    {
        public string DayName { get; set; } = "";
        public int TotalAppointments { get; set; }
        public int CompletedVisits { get; set; }
        public int CapacityPercentage { get; set; }
    }

    public class OperationalDoctorStatDto
    {
        public Guid DoctorId { get; set; }
        public string Name { get; set; } = "";
        public string Specialty { get; set; } = "";
        public int TotalAppointments { get; set; }
        public int CompletedVisits { get; set; }
        public int CancelledCount { get; set; }
        public int NoShowCount { get; set; }
        public double CompletionPercentage { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class OperationalVisitLogDto
    {
        public Guid AppointmentId { get; set; }
        public string PatientName { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string Specialty { get; set; } = "";
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public int Status { get; set; }
    }
}
