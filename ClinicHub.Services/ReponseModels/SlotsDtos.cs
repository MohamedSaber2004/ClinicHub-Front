namespace ClinicHub.Services.ReponseModels
{
    /// <summary>
    /// Response of GET /api/v1/clinics/{clinicId}/doctors/{doctorId}/slots?date=YYYY-MM-DD
    /// One <see cref="SlotDayDto"/> per availability row — the same weekday may appear
    /// multiple times (shifts) each with its own <see cref="SlotDayDto.SlotDurationMinutes"/>.
    /// </summary>
    public class AvailableSlotsDto
    {
        public Guid DoctorId { get; set; }
        public Guid ClinicId { get; set; }
        public string? RequestedDate { get; set; }
        public List<SlotDayDto> Days { get; set; } = new();
    }

    public class SlotDayDto
    {
        /// <summary>Day name as returned by the API (e.g. "Monday"). Numeric values are tolerated.</summary>
        public string DayOfWeek { get; set; } = "";

        public WorkingHoursDto? WorkingHours { get; set; }

        /// <summary>The booking duration used to generate this segment's slots — always read from the API.</summary>
        public int SlotDurationMinutes { get; set; }

        public List<SlotDto> Slots { get; set; } = new();
    }

    public class WorkingHoursDto
    {
        public string? From { get; set; }
        public string? To { get; set; }
    }

    public class SlotDto
    {
        public Guid Id { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }

        /// <summary>false = the slot overlaps an already-booked (non-cancelled) appointment.</summary>
        public bool IsAvailable { get; set; }
    }
}
