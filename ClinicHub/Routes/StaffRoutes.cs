namespace ClinicHub.Routes
{
    public static class StaffRoutes
    {
        public static string Base => "/Staff";

        public static class Pages
        {
            public static string Index() => $"{Base}/Index";
            public static string Appointments() => $"{Base}/Appointments";
            public static string Queue() => $"{Base}/Queue";
            public static string RegisterPatient() => $"{Base}/RegisterPatient";
            public static string DoctorSchedule(int doctorId) => $"{Base}/DoctorSchedule/{doctorId}";
        }
    }
}
