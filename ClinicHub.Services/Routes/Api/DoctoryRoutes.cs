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
            Specializations = new SpecializationRoutes(BaseRoute);
            Attachments = new AttachmentRoutes(BaseRoute);
        }

        public static AuthRoutes Auth { get; private set; } = null!;
        public static SpecializationRoutes Specializations { get; private set; } = null!;
        public static AttachmentRoutes Attachments { get; private set; } = null!;

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
            public string Logout => $"{BaseRoute}/logout";
        }

        public class SpecializationRoutes
        {
            public string BaseRoute { get; }

            public SpecializationRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/specializations";
            }

            public string GetAll => $"{BaseRoute}";
            public string GetById(Guid id) => $"{BaseRoute}/{id}";
            public string Create => $"{BaseRoute}/create";
            public string Update => $"{BaseRoute}/update";
            public string Delete => $"{BaseRoute}/delete";
        }

        public class AttachmentRoutes
        {
            public string BaseRoute { get; }

            public AttachmentRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/attachments";
            }

            public string Upload => $"{BaseRoute}/upload";
            public string Update(string name) => $"{BaseRoute}/update/{name}";
            public string UploadMultiple => $"{BaseRoute}/upload-multiple-attachments";
            public string Download => $"{BaseRoute}/download";
        }
    }
}
