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
            public static string Subscriptions() => $"{Base}/Subscriptions";
            public static string ClinicRegister() => $"{Base}/ClinicRegister";
            public static string RegistrationSubmitted() => $"{Base}/RegistrationSubmitted";
            public static string PendingApproval() => $"{Base}/PendingApproval";
            public static string SubscriptionRequired() => $"{Base}/SubscriptionRequired";
            public static string PaymentResult() => $"{Base}/PaymentResult";
        }

        public static class Account
        {
            private static string AccountBase => "/Account";

            public static string Login() => $"{AccountBase}/Login";
            public static string Logout() => $"{AccountBase}/Logout";
            public static string RefreshToken() => $"{AccountBase}/RefreshToken";
            public static string ForgotPassword() => $"{AccountBase}/ForgotPassword";
            public static string VerifyCode() => $"{AccountBase}/VerifyCode";
            public static string ResetPassword() => $"{AccountBase}/ResetPassword";
        }
    }
}
