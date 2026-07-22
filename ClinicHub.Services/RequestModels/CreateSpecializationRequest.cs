using Microsoft.AspNetCore.Http;

namespace ClinicHub.Services.RequestModels
{
    public record CreateSpecializationRequest(
        string Name,
        string ArName,
        string? Description,
        string? Icon,
        bool IsFamous,
        bool IsActive = true);
}
