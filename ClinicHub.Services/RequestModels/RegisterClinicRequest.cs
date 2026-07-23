using System;
using System.Collections.Generic;
using ClinicHub.Services.Enums;
using Newtonsoft.Json;

namespace ClinicHub.Services.RequestModels
{
    public class RegisterClinicRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;
        public DateTime? BirthDate { get; set; }
        public Gender? Gender { get; set; } = Enums.Gender.Male;
        public string? FcmToken { get; set; }
        public int? DevicePlatform { get; set; }

        public string ClinicName { get; set; } = null!;
        public string? ClinicNameAr { get; set; }
        public string? ClinicDescription { get; set; }
        public string? ClinicAddress { get; set; }
        public string? ClinicPhone { get; set; }
        public string? ClinicEmail { get; set; }
        public Guid SpecializationId { get; set; }
        public string? WorkingHours { get; set; }
        public string? WorkingHoursStart { get; set; }
        public string? WorkingHoursEnd { get; set; }
        public List<string>? WorkingDays { get; set; }

        [JsonProperty("lat")]
        public double? Lat { get; set; }

        [JsonProperty("lng")]
        public double? Lng { get; set; }

        public string? Logo { get; set; }
        public string? ProfessionalPracticeCardImage { get; set; }
        public string? TaxCardImage { get; set; }
        public string? UnionIdCardImage { get; set; }

        [JsonProperty("doctorImage")]
        public string? DoctorImage { get; set; }

        public string? Bio { get; set; }
        public int? YearsOfExperience { get; set; }

        [JsonIgnore]
        public double? Latitude
        {
            get => Lat;
            set => Lat = value;
        }

        [JsonIgnore]
        public double? Longitude
        {
            get => Lng;
            set => Lng = value;
        }

        [JsonIgnore]
        public string? ImageUrl
        {
            get => DoctorImage;
            set => DoctorImage = value;
        }
    }
}
