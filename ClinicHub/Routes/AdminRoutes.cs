namespace ClinicHub.Routes
{
    public static class AdminRoutes
    {
        public static string Base => "/Admin";

        public static class Pages
        {
            public static string Index() => $"{Base}/Index";
        }
    }
}
