namespace ClinicHub.Services.Routes.Api
{
    public static class DoctoryRoutes
    {
        public static string Version => "v1";
        public static string BaseRoute { get; private set; } = string.Empty;

        public static void Initialize(string baseUrl)
        {
            BaseRoute = $"{baseUrl}/api/{Version}";
            Auth = new AuthRoutes(BaseRoute);
        }

        public static AuthRoutes Auth { get; private set; } = null!;

        public class AuthRoutes
        {
            public string BaseRoute { get; }

            public AuthRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/auth";
            }

            public string Login => $"{BaseRoute}/login";
            public string ForgetPassword => $"{BaseRoute}/forget-password";
            public string VerifyResetToken => $"{BaseRoute}/verify-reset-token";
            public string ResetPassword => $"{BaseRoute}/reset-password";
            public string RefreshToken => $"{BaseRoute}/refresh-token";
        }
    }
}
