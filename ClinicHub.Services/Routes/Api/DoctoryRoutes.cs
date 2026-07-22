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
            Verification = new VerificationRoutes(BaseRoute);
            Users = new UserRoutes(BaseRoute);
            Doctors = new DoctorRoutes(BaseRoute);
            Clinics = new ClinicRoutes(BaseRoute);
        }

        public static AuthRoutes Auth { get; private set; } = null!;
        public static SpecializationRoutes Specializations { get; private set; } = null!;
        public static AttachmentRoutes Attachments { get; private set; } = null!;
        public static VerificationRoutes Verification { get; private set; } = null!;
        public static UserRoutes Users { get; private set; } = null!;
        public static DoctorRoutes Doctors { get; private set; } = null!;
        public static ClinicRoutes Clinics { get; private set; } = null!;

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

        public class VerificationRoutes
        {
            public string BaseRoute { get; }

            public VerificationRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/admin/users";
            }

            public string GetPendingVerifications => $"{BaseRoute}/pending";
            public string ApproveUserVerification(Guid id) => $"{BaseRoute}/{id}/approve";
            public string RejectUserVerification(Guid id) => $"{BaseRoute}/{id}/reject";
        }

        public class UserRoutes
        {
            public string BaseRoute { get; }
            public UserRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/users";
            }

            public string GetAll => $"{BaseRoute}";
            public string ChangePassword => $"{BaseRoute}/change-password";
            public string Create => $"{BaseRoute}";
            public string Delete(Guid id) => $"{BaseRoute}/{id}";
            public string EditUser(Guid id) => $"{BaseRoute}/{id}";
        }

        public class DoctorRoutes
        {
            public string BaseRoute { get; }
            public DoctorRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/admin/dashboard";
            }

            public string GetAllClinicsForViewingOnly => $"{BaseRoute}/clinics";
        }

        public class ClinicRoutes
        {
            public string BaseRoute { get; }
            public ClinicRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/admin/clinics";
            }
            public string GetAll => $"{BaseRoute}/paginated";
            public string GetById(Guid id) => $"{BaseRoute}/{id}";
            public string Create => $"{BaseRoute}";
            public string Update(Guid id) => $"{BaseRoute}/{id}";
            public string Activate(Guid id) => $"{BaseRoute}/{id}/activate";
            public string Deactivate(Guid id) => $"{BaseRoute}/{id}/deactivate";

        }
    }
}
