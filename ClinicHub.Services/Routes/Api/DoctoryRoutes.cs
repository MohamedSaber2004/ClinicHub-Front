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
            StaffDashboard = new StaffDashboardRoutes(BaseRoute);
            Staff = new StaffRoutes(BaseRoute);
            Clinics = new ClinicRoutes(BaseRoute);
            Plans = new PlanRoutes(BaseRoute);
            Subscriptions = new SubscriptionRoutes(BaseRoute);
            AdminSubscriptions = new AdminSubscriptionRoutes(BaseRoute);
        }

        public static StaffDashboardRoutes StaffDashboard { get; private set; } = null!;
        public static AuthRoutes Auth { get; private set; } = null!;
        public static SpecializationRoutes Specializations { get; private set; } = null!;
        public static AttachmentRoutes Attachments { get; private set; } = null!;
        public static VerificationRoutes Verification { get; private set; } = null!;
        public static UserRoutes Users { get; private set; } = null!;
        public static DoctorRoutes Doctors { get; private set; } = null!;
        public static StaffRoutes Staff { get; private set; } = null!;
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
            private readonly string _baseRoute;
            public string BaseRoute { get; }
            public string AdminClinicRoute { get; }
            public DoctorRoutes(string baseRoute)
            {
                _baseRoute = baseRoute;
                BaseRoute = $"{baseRoute}/admin/dashboard";
                AdminClinicRoute = $"{baseRoute}/admin/clinics";
            }

            public string GetAllClinicsForViewingOnly => $"{BaseRoute}/clinics";
            public string ListByClinic(Guid clinicId) => $"{AdminClinicRoute}/{clinicId}/doctors";
            public string GetById(Guid id) => $"{_baseRoute}/doctors/{id}";
            public string Create => $"{AdminClinicRoute}/doctors";
            public string Update(Guid id) => $"{_baseRoute}/doctors/{id}";
            public string Delete(Guid id) => $"{_baseRoute}/doctors/{id}";
            public string ChangePassword(Guid id) => $"{AdminClinicRoute}/doctors/{id}/change-password";
        }

        public class StaffDashboardRoutes
        {
            public string BaseRoute { get; }
            public StaffDashboardRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/staff";
            }

            public string Stats => $"{BaseRoute}/dashboard/stats";
            public string Queue => $"{BaseRoute}/queue";
            public string Appointments => $"{BaseRoute}/appointments";
            public string Approve(string id) => $"{BaseRoute}/appointments/{id}/approve";
            public string Reject(string id) => $"{BaseRoute}/appointments/{id}/reject";
            public string CheckIn(string id) => $"{BaseRoute}/appointments/{id}/check-in";
            public string Complete(string id) => $"{BaseRoute}/appointments/{id}/complete";
            public string RegisterPatient => $"{BaseRoute}/patients/register";
            public string Doctors => $"{BaseRoute}/doctors";
            public string DoctorSchedule(string doctorId) => $"{BaseRoute}/doctors/{doctorId}/schedule";
        }

        public class StaffRoutes
        {
            public string BaseRoute { get; }
            public StaffRoutes(string baseRoute)
            {
                BaseRoute = $"{baseRoute}/admin/clinics/staff";
            }

            public string List => $"{BaseRoute}";
            public string GetById(Guid id) => $"{BaseRoute}/{id}";
            public string Create => $"{BaseRoute}";
            public string Update(Guid id) => $"{BaseRoute}/{id}";
            public string Delete(Guid id) => $"{BaseRoute}/{id}";
            public string ChangePassword(Guid id) => $"{BaseRoute}/{id}/change-password";
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
