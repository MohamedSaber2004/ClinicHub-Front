using ClinicHub.Services.Enums;

namespace ClinicHub.Services.RequestModels
{
    public class GetAllClinicsPagginatedRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool SortAscending { get; set; } = true;
        public string? SearchTerm { get; set; }
        public ClinicStatus? Status { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
