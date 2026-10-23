using System;

namespace ClinicHub.Services.ReponseModels
{
    public class PatientAssessmentDto
    {
        public Guid? Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public Guid? ClinicId { get; set; }
        public Guid? AppointmentId { get; set; }
        public string AssessmentDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

        // PERSONAL HISTORY
        public string PatientName { get; set; } = string.Empty;
        public int? Age { get; set; }
        public int? Gender { get; set; } // 0: Male, 1: Female
        public string? Occupation { get; set; }
        public string? Handedness { get; set; } // Right, Left, Ambidextrous
        public string? SpecialHabits { get; set; }
        public string? ContactNumber { get; set; }
        public string? Address { get; set; }
        public double? HeightCm { get; set; }
        public double? WeightKg { get; set; }
        public double? Bmi { get; set; }

        // DIAGNOSIS
        public string? Diagnosis { get; set; }

        // CHIEF COMPLAIN
        public string? ChiefComplaint { get; set; }

        // PRESENT HISTORY
        public string? OnsetOfSymptoms { get; set; }
        public string? DurationOfSymptoms { get; set; }
        public string? CourseOfSymptoms { get; set; }
        public string? MechanismOfInjury { get; set; }
        public string? DiagnosticImagingAndLab { get; set; }
        public string? PreviousTreatment { get; set; }
        public string? DateOfAdmission { get; set; }
        public string? DateAndTypeOfSurgery { get; set; }
        public string? SpecialPrecautions { get; set; }

        // MEDICAL HISTORY
        public string? MedicalHistory { get; set; }

        // FAMILY HISTORY
        public string? FamilyHistory { get; set; }

        // TREATMENT PLAN (under family history)
        public string? TreatmentPlan { get; set; }
        public string? Medications { get; set; }
        public string? PhysicalTherapy { get; set; }
        public string? TreatmentDuration { get; set; }
        public string? FollowUpNotes { get; set; }
    }
}
