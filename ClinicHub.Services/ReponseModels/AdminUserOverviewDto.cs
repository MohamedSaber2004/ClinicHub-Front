namespace ClinicHub.Services.ReponseModels
{
    public class AdminUserOverviewDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();

        public int TotalAppointments { get; set; }
        public int TotalVisits { get; set; }
        public double? AvgRating { get; set; }
        public int ReviewCount { get; set; }
        public decimal TotalSpent { get; set; }

        public List<AdminUserVisitDto> RecentVisits { get; set; } = new();
        public List<AdminUserPaymentDto> Payments { get; set; } = new();
        public List<AdminUserRequestDto> Requests { get; set; } = new();
    }

    public class AdminUserVisitDto
    {
        public Guid AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public string DoctorName { get; set; } = "";
        public string Specialty { get; set; } = "";
        public int Status { get; set; }
    }

    public class AdminUserPaymentDto
    {
        public Guid PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public int Type { get; set; }
        public int Status { get; set; }
        public int Method { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminUserRequestDto
    {
        public Guid RequestId { get; set; }
        public int RequestedRole { get; set; }
        public int Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
