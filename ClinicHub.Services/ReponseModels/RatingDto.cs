namespace ClinicHub.Services.ReponseModels
{
    public class RatingDto
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public Guid? DoctorId { get; set; }
        public Guid? ClinicId { get; set; }
        public int Value { get; set; }
        public string? Review { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
