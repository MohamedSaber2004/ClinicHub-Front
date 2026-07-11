using ClinicHub.Services.Enums;

namespace ClinicHub.Services.RequestModels
{
    public class GetAllUsersRequest
    {
        public Guid? UserId { get; set; }
        public string? SearchTerm { get; set; }
        public UserType? UserType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
