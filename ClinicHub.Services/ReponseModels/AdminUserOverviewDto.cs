using Newtonsoft.Json;

namespace ClinicHub.Services.ReponseModels
{
    public class AdminUserOverviewDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Image { get; set; }
        public string? ImageUrl { get; set; }
        [JsonProperty("imageName")]
        public string? ImageName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
        // Optional: sent by the backend when the user follows a clinic.
        // Null when freelance (or when the endpoint omits it) — absence alone
        // never proves freelance; only a present value proves affiliation.
        public Guid? ClinicId { get; set; }
        // Authoritative backend verdict (false by default on older backends).
        public bool IsFreelanceDoctor { get; set; }

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
