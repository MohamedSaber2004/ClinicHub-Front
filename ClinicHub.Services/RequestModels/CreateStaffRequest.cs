namespace ClinicHub.Services.RequestModels
{
    public class CreateStaffRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;
        public Guid ClinicId { get; set; }
    }
}