using ClinicHub.Services.Enums;

namespace ClinicHub.Services.RequestModels
{
    public class EditUserRoleRequest
    {
        public Guid Id { get; set; }
        public UserType Role { get; set; }
    }
}
