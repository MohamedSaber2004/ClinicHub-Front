namespace ClinicHub.Services.RequestModels
{
    public class CreateDoctorRequest
    {
        public Guid ClinicId { get; set; }
        public Guid UserId { get; set; }
        public Guid SpecializationId { get; set; }
        public string? Bio { get; set; }
        public int YearsOfExperience { get; set; }
    }
}