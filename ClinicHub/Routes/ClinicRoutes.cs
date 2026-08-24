namespace ClinicHub.Routes
{
    public static class ClinicRoutes
    {
        public static string Base => "/Clinic";

        public static class Pages
        {
            public static string Index() => $"{Base}/Index";
            public static string AppointmentRevenue() => $"{Base}/AppointmentRevenue";
            public static string MedicalRecords() => $"{Base}/MedicalRecords";
            public static string PatientPortal() => $"{Base}/PatientPortal";
            public static string Staff() => $"{Base}/Staff";
            public static string Doctors() => $"{Base}/Doctors";
            public static string Reports() => $"{Base}/Reports";
            public static string Ratings() => $"{Base}/Ratings";
            public static string Marketing() => $"{Base}/Marketing";
            public static string Settings() => $"{Base}/Settings";
            public static string MySubscription() => $"{Base}/MySubscription";
            public static string Profile() => $"{Base}/Profile";
            public static string Subscribe() => $"{Base}/Subscribe";
            public static string CancelSubscription() => $"{Base}/CancelSubscription";
            public static string DoctorAppointments() => $"{Base}/DoctorAppointments";
            public static string DoctorPatients() => $"{Base}/DoctorPatients";
            public static string DoctorPatientHistory(Guid patientId) => $"{Base}/DoctorPatientHistory/{IdProtector.Protect(patientId)}";
            public static string DoctorPatientHistory(string token) => $"{Base}/DoctorPatientHistory/{token}";
            public static string DoctorAvailability() => $"{Base}/DoctorAvailability";
            public static string Notifications() => $"{Base}/Notifications";
        }
    }
}
