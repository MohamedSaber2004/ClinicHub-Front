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
        ManageAds = 1L << 6,
        ManageSpecializations = 1L << 7,
        ReviewPendingClinics = 1L << 8,
        ManageSupportTickets = 1L << 9,

        ViewClinicDashboard = 1L << 10,
        ManageClinicSettings = 1L << 11,
        ManageClinicLocation = 1L << 12,
        ManageAppointments = 1L << 13,
        ManageMedicalRecords = 1L << 14,
        ManageBilling = 1L << 15,
        ManageInventory = 1L << 16,
        ManageClinicStaff = 1L << 17,
        ManageClinicDoctors = 1L << 18,

        BookAppointment = 1L << 19,
        ViewOwnMedicalRecords = 1L << 20,
        RateClinic = 1L << 21,
        ViewOwnBilling = 1L << 22,
    }

    public static class RolePermissions
    {
        public static Permission For(UserRole role, DoctorEmploymentType? doctorType = null)
        {
            if (role == UserRole.Doctor && doctorType == DoctorEmploymentType.Freelance)
            {
                return Permission.BookAppointment | Permission.ViewOwnMedicalRecords |
                       Permission.RateClinic | Permission.ViewOwnBilling;
            }

            return role switch
            {
                UserRole.SystemAdmin =>
                    Permission.ViewAdminDashboard | Permission.ManageClinics | Permission.ManageDoctors |
                    Permission.ManageUsers | Permission.ManageSubscriptions | Permission.ManagePayments |
                    Permission.ManageAds | Permission.ManageSpecializations | Permission.ReviewPendingClinics |
                    Permission.ManageSupportTickets,

                UserRole.ClinicOwner =>
                    Permission.ViewClinicDashboard | Permission.ManageClinicSettings | Permission.ManageClinicLocation |
                    Permission.ManageAppointments | Permission.ManageMedicalRecords | Permission.ManageBilling |
                    Permission.ManageInventory | Permission.ManageClinicStaff | Permission.ManageClinicDoctors,

                UserRole.ClinicManager =>
                    Permission.ViewClinicDashboard | Permission.ManageAppointments | Permission.ManageMedicalRecords |
                    Permission.ManageInventory | Permission.ManageClinicStaff,

                UserRole.Doctor =>
                    Permission.ViewClinicDashboard | Permission.ManageAppointments | Permission.ManageMedicalRecords,

                UserRole.ClinicStaff =>
                    Permission.ManageAppointments,

                UserRole.Patient =>
                    Permission.BookAppointment | Permission.ViewOwnMedicalRecords |
                    Permission.RateClinic | Permission.ViewOwnBilling,

                _ => Permission.None
            };
        }
    }
}
