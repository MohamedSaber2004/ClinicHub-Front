namespace ClinicHub.Services.ReponseModels
{
    public class UserListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Initials { get; set; } = "";
        public string? Image { get; set; }
        public string RegistrationDate { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusClass { get; set; } = "";
        public string Role { get; set; } = "";
        public List<string> Roles { get; set; } = new();
    }
}
