namespace ClinicHub.Services.RequestModels
{
    /// <summary>
    /// Patient booking payload. The frontend must submit exactly the slot times returned
    /// by the slots endpoint (startTime + endTime) — made-up times are rejected with HTTP 400.
    /// </summary>
    public class BookAppointmentRequest
    {
        public Guid ClinicId { get; set; }
        public Guid DoctorId { get; set; }

        /// <summary>Booking date in YYYY-MM-DD format.</summary>
        public string Date { get; set; } = "";

        /// <summary>Slot start time exactly as returned by the slots endpoint.</summary>
        public string StartTime { get; set; } = "";

        /// <summary>Slot end time exactly as returned by the slots endpoint.</summary>
        public string EndTime { get; set; } = "";

        public string? PatientName { get; set; }
        public string? PatientPhone { get; set; }
    }
}
