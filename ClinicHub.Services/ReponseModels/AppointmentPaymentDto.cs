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

        // Breakdown from backend when available (same pattern as AdminPaymentDto).
        // Clinic owner UI must display only the clinic net — never the platform fee.
        // Nullable + tolerant deserialization: older backend payloads that only
        // send Amount keep working via the NetAmount fallback below.
        public decimal? PlatformFee { get; set; }
        public decimal? ClinicNetAmount { get; set; }

        /// <summary>
        /// Amount actually owed to the clinic (without admin platform fees).
        /// Prefers the backend-provided net; falls back to Amount for legacy payloads.
        /// </summary>
        public decimal NetAmount => ClinicNetAmount ?? Amount;
    }
}
