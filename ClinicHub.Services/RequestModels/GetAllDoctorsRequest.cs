namespace ClinicHub.Services.RequestModels
{
using ClinicHub.Services.Enums;

public class GetAllDoctorsRequest
{
    public string? SearchTerm { get; set; }
    public bool? IsUnassigned { get; set; }
    public Guid? ClinicId { get; set; }
    public List<UserType>? UserTypes { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
}
