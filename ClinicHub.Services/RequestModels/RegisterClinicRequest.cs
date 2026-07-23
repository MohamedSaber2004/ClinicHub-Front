namespace ClinicHub.Services.RequestModels
{
    public class RegisterClinicRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ClinicName { get; set; } = null!;
        public string ClinicAddress { get; set; } = null!;
        public Guid SpecializationId { get; set; }
    }
}
