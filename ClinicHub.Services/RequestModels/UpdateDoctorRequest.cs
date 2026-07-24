namespace ClinicHub.Services.RequestModels
{
    public class UpdateDoctorRequest
    {
        public bool? IsActive { get; set; }
        public string? Bio { get; set; }
        public int? YearsOfExperience { get; set; }
    }
}