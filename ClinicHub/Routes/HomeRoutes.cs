namespace ClinicHub.Routes
{
    public static class HomeRoutes
    {
        public static string Base => "/Home";

        public static class Pages
        {
            public static string Index() => "/";
            public static string Privacy() => $"{Base}/Privacy";
            public static string About() => $"{Base}/About";
        }

        public static class Account
        {
            private static string AccountBase => "/Account";

            public static string Login() => $"{AccountBase}/Login";
            public static string ForgotPassword() => $"{AccountBase}/ForgotPassword";
            public static string VerifyCode() => $"{AccountBase}/VerifyCode";
            public static string ResetPassword() => $"{AccountBase}/ResetPassword";
        }
    }
}
