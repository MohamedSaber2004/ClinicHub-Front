namespace ClinicHub.Routes
{
    public static class ClinicRoutes
    {
        public static string Base => "/Clinic";

        public static class Pages
        {
            public static string Index() => $"{Base}/Index";
        }
    }
}
