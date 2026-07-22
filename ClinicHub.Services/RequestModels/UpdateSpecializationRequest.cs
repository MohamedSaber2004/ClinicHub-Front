using Microsoft.AspNetCore.Http;

namespace ClinicHub.Services.RequestModels
{
    public class UpdateSpecializationRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string ArName { get; set; } = null!;
        public string? Description { get; set; }
        public IFormFile? Icon { get; set; }
        public string? CurrentIcon { get; set; }
        public bool IsFamous { get; set; }
        public bool IsActive { get; set; }
    }
}
