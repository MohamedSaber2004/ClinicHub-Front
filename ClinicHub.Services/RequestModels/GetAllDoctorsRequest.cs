namespace ClinicHub.Services.RequestModels
{
    public class GetAllDoctorsRequest
    {
    public string? SearchTerm { get; set; }
    public bool? IsUnassigned { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
}
