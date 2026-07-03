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
            public static string Kpi() => $"{Base}/Kpi";
            public static string Support() => $"{Base}/Support";
            public static string Ads() => $"{Base}/Ads";
            public static string Profile() => $"{Base}/Profile";
        }
    }
}
