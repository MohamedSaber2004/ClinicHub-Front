namespace ClinicHub.Services.RequestModels
{
    public class CreateClinicRequest
    {
        public string? NameAr {get;set;}
        public string? ArDescription{get;set;}
        public string? AddressAr {get;set;}
        public string? Phone {get;set;}
        public string Email { get; set; } = null!;
        public string? Website {get;set;}
        public string? Logo {get;set;}
        public string? WorkingHours {get;set;}
        public Guid SpecializationId {get;set;}
        public double? Lat {get;set;}
        public double? Lng { get; set;}
        public string OwnerName { get; set; } = null!;
        public string OwnerEmail {get;set;} = null!;
        public string? OwnerPhone {get;set;}
        public string? WorkingHoursStart {get;set;}
        public string? WorkingHoursEnd {get;set;}
        public List<DayOfWeek>? WorkingDays {get;set;}
        public string? Bio { get;set;}
        public int YearsOfExperience { get;set;}
        public Guid DoctorSpecializationId { get;set;}
    }
}
