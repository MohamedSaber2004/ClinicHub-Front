namespace ClinicHub.Routes
{
    public static class ClinicRoutes
    {
        public static string Base => "/Clinic";

        public static class Pages
        {
            public static string Index() => $"{Base}/Index";
            public static string Appointments() => $"{Base}/Appointments";
            public static string MedicalRecords() => $"{Base}/MedicalRecords";
            public static string Billing() => $"{Base}/Billing";
            public static string Inventory() => $"{Base}/Inventory";
            public static string PatientPortal() => $"{Base}/PatientPortal";
            public static string Staff() => $"{Base}/Staff";
            public static string Settings() => $"{Base}/Settings";
            public static string MySubscription() => $"{Base}/MySubscription";
            public static string Subscribe() => $"{Base}/Subscribe";
            public static string CancelSubscription() => $"{Base}/CancelSubscription";
        }
    }
}
