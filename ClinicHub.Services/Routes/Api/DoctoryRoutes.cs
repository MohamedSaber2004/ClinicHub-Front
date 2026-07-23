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
            Plans = new PlanRoutes(BaseRoute);
            Subscriptions = new SubscriptionRoutes(BaseRoute);
            AdminSubscriptions = new AdminSubscriptionRoutes(BaseRoute);
        }

        public static AuthRoutes Auth { get; private set; } = null!;
        public static SpecializationRoutes Specializations { get; private set; } = null!;
        public static AttachmentRoutes Attachments { get; private set; } = null!;
        public static VerificationRoutes Verification { get; private set; } = null!;
        public static UserRoutes Users { get; private set; } = null!;
        public static DoctorRoutes Doctors { get; private set; } = null!;
        public static ClinicRoutes Clinics { get; private set; } = null!;
        public static PlanRoutes Plans { get; private set; } = null!;
        public static SubscriptionRoutes Subscriptions { get; private set; } = null!;
        public static AdminSubscriptionRoutes AdminSubscriptions { get; private set; } = null!;

        public class AuthRoutes
        {
            public string BaseRoute { get; }

            public AuthRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/auth";
            }

            public string Login => $"{BaseRoute}/login-web";
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

            public string GetActive => $"{BaseRoute}/active";
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
            public string AdminBaseRoute { get; }
            public ClinicRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/clinics";
                AdminBaseRoute = $"{baseRoute}/admin/clinics";
            }
            public string Register => $"{BaseRoute}/register";
            public string GetAll => $"{AdminBaseRoute}/paginated";
            public string GetById(Guid id) => $"{AdminBaseRoute}/{id}";
            public string Create => $"{AdminBaseRoute}";
            public string Update(Guid id) => $"{AdminBaseRoute}/{id}";
            public string Activate(Guid id) => $"{AdminBaseRoute}/{id}/activate";
            public string Deactivate(Guid id) => $"{AdminBaseRoute}/{id}/deactivate";
        }

        public class PlanRoutes
        {
            public string BaseRoute { get; }
            public PlanRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/plans";
            }
            public string List => $"{BaseRoute}";
        }

        public class SubscriptionRoutes
        {
            public string BaseRoute { get; }
            public SubscriptionRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/subscriptions";
            }
            public string My => $"{BaseRoute}/my";
            public string InitiatePayment => $"{BaseRoute}/initiate-payment";
            public string Cancel => $"{BaseRoute}/my/cancel";
        }

        public class AdminSubscriptionRoutes
        {
            public string DashboardRoute { get; }
            public string AdminRoute { get; }
            public AdminSubscriptionRoutes(string baseRoute)
            {
                DashboardRoute = $"{baseRoute}/admin/dashboard";
                AdminRoute = $"{baseRoute}/admin";
            }
            public string PendingClinics => $"{DashboardRoute}/clinics/pending";
            public string ApproveClinic(Guid id) => $"{DashboardRoute}/clinics/{id}/approve";
            public string RejectClinic(Guid id) => $"{DashboardRoute}/clinics/{id}/reject";
            public string ListPlans => $"{AdminRoute}/plans";
            public string CreatePlan => $"{AdminRoute}/plans";
            public string UpdatePlan(Guid id) => $"{AdminRoute}/plans/{id}";
            public string DeletePlan(Guid id) => $"{AdminRoute}/plans/{id}";
            public string ListSubscriptions => $"{DashboardRoute}/subscriptions";
            public string RevokeSubscription(Guid id) => $"{DashboardRoute}/subscriptions/{id}/revoke";
        }
    }
}
