namespace ClinicHub.Data
{
    public enum UserRole
    {
        SystemAdmin,
        ClinicOwner,
        ClinicManager,
        Doctor,
        ClinicStaff,
        Patient
    }

    public enum ClinicStaffRole
    {
        Reception,
        Nurse,
        Cleaner,
        Helper
    }

    public enum DoctorEmploymentType
    {
        Freelance,
        OwnClinic,
        InCenter
    }

    [Flags]
    public enum Permission : long
    {
        None = 0,

        ViewAdminDashboard = 1L << 0,
        ManageClinics = 1L << 1,
        ManageDoctors = 1L << 2,
        ManageUsers = 1L << 3,
        ManageSubscriptions = 1L << 4,
        ManagePayments = 1L << 5,
        ManageSpecializations = 1L << 6,
        ReviewPendingClinics = 1L << 8,

        ViewClinicDashboard = 1L << 10,
        ManageClinicSettings = 1L << 11,
        ManageClinicLocation = 1L << 12,
        ManageAppointments = 1L << 13,
        ManageMedicalRecords = 1L << 14,
        ManageClinicStaff = 1L << 17,
        ManageClinicDoctors = 1L << 18,

        BookAppointment = 1L << 19,
        ViewOwnMedicalRecords = 1L << 20,
        RateClinic = 1L << 21,
    }

    [Flags]
    public enum PlanFeature : long
    {
        None = 0,
        ManageAppointments = 1L << 0,
        ManagePatientRecords = 1L << 1,
        BasicReports = 1L << 2,
        AdvancedReports = 1L << 3,
        MarketingTools = 1L << 4,
        PrioritySupport = 1L << 5,
        OnlineBooking = 1L << 6,
        ManageStaff = 1L << 7,
        ManageDoctors = 1L << 8,
    }

    public static class PlanFeatureMap
    {
        private static readonly Dictionary<string, PlanFeature> FeatureKeyMap = new()
        {
            ["appointments"] = PlanFeature.ManageAppointments,
            ["patient_records"] = PlanFeature.ManagePatientRecords,
            ["basic_reports"] = PlanFeature.BasicReports,
            ["advanced_reports"] = PlanFeature.AdvancedReports,
            ["marketing_tools"] = PlanFeature.MarketingTools,
            ["priority_support"] = PlanFeature.PrioritySupport,
            ["online_booking"] = PlanFeature.OnlineBooking,
            ["staff_management"] = PlanFeature.ManageStaff,
            ["doctor_management"] = PlanFeature.ManageDoctors,
        };

        public static PlanFeature FromFeatureStrings(List<string> features)
        {
            var result = PlanFeature.None;
            foreach (var key in features)
            {
                if (FeatureKeyMap.TryGetValue(key, out var feature))
                {
                    result |= feature;
                }
            }
            return result;
        }
    }

    public static class RolePermissions
    {
        public static Permission For(UserRole role, DoctorEmploymentType? doctorType = null)
        {
            if (role == UserRole.Doctor && doctorType == DoctorEmploymentType.Freelance)
            {
                return Permission.BookAppointment | Permission.ViewOwnMedicalRecords |
                       Permission.RateClinic;
            }

            return role switch
            {
                UserRole.SystemAdmin =>
                    Permission.ViewAdminDashboard | Permission.ManageClinics | Permission.ManageDoctors |
                    Permission.ManageUsers | Permission.ManageSubscriptions | Permission.ManagePayments |
                    Permission.ManageSpecializations | Permission.ReviewPendingClinics,

                UserRole.ClinicOwner =>
                    Permission.ViewClinicDashboard | Permission.ManageClinicSettings | Permission.ManageClinicLocation |
                    Permission.ManageAppointments | Permission.ManageMedicalRecords |
                    Permission.ManageClinicStaff | Permission.ManageClinicDoctors,

                UserRole.ClinicManager =>
                    Permission.ViewClinicDashboard | Permission.ManageAppointments | Permission.ManageMedicalRecords |
                    Permission.ManageClinicStaff,

                UserRole.Doctor =>
                    Permission.ViewClinicDashboard | Permission.ManageAppointments | Permission.ManageMedicalRecords,

                UserRole.ClinicStaff =>
                    Permission.ManageAppointments,

                UserRole.Patient =>
                    Permission.BookAppointment | Permission.ViewOwnMedicalRecords |
                    Permission.RateClinic,

                _ => Permission.None
            };
        }
    }
}
