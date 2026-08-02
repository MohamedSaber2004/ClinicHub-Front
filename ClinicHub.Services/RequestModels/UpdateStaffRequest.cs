namespace ClinicHub.Services.RequestModels
{
    public class UpdateStaffRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
        public string? Image { get; set; }
    }
}