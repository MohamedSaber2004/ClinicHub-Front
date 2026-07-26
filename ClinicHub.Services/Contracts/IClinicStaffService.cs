using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Services.Contracts
{
    public interface IClinicStaffService
    {
        Task<PagginatedResult<StaffDto>> GetStaffAsync(int pageNumber = 1, int pageSize = 20, string? searchTerm = null, bool? isActive = null);
        Task<StaffDto?> GetStaffByIdAsync(Guid id);
        Task<Guid> CreateStaffAsync(CreateStaffRequest request);
        Task<bool> UpdateStaffAsync(Guid id, UpdateStaffRequest request);
        Task<bool> DeleteStaffAsync(Guid id);
        Task<bool> ChangeStaffPasswordAsync(Guid id, ChangePasswordRequest request);
    }
}