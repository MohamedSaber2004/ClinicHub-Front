namespace ClinicHub.Routes
{
    public static class AdminRoutes
    {
        public static string Base => "/Admin";

        public static class Pages
        {
            public static string Index() => $"{Base}/Index";
            public static string Specializations() => $"{Base}/Specializations";
            public static string Clinics() => $"{Base}/Clinics";
            public static string Doctors() => $"{Base}/Doctors";
            public static string Support() => $"{Base}/Support";
            public static string Ads() => $"{Base}/Ads";
            public static string Payments() => $"{Base}/Payments";
            public static string PaymentsDetails(int id) => $"{Base}/PaymentsDetails/{id}";
            public static string Profile() => $"{Base}/Profile";
        }
    }
}
