namespace ClinicHub.Routes
{
    public static class DoctorRoutes
    {
        public static string Base => "/Doctor";

        public static class Pages
        {
            public static string Index() => $"{Base}/Index";
            public static string Appointments() => $"{Base}/Appointments";
            public static string Patients() => $"{Base}/Patients";
            public static string PatientHistory(int patientId) => $"{Base}/PatientHistory/{patientId}";
            public static string Availability() => $"{Base}/Availability";
            public static string Profile() => $"{Base}/Profile";
            public static string Notifications() => $"{Base}/Notifications";
        }
    }
}
