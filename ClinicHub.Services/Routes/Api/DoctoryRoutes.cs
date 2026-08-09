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
            AdminDashboard = new AdminDashboardRoutes(BaseRoute);
            DoctorDashboard = new DoctorDashboardRoutes(BaseRoute);
            ClinicDashboard = new ClinicDashboardRoutes(BaseRoute);
            AdminPayments = new AdminPaymentsRoutes(BaseRoute);
            AdminAds = new AdminAdsRoutes(BaseRoute);
            Ads = new AdsRoutes(BaseRoute);
            Notifications = new NotificationsRoutes(BaseRoute);
            Ratings = new RatingsRoutes(BaseRoute);
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
        public static AdminDashboardRoutes AdminDashboard { get; private set; } = null!;
        public static DoctorDashboardRoutes DoctorDashboard { get; private set; } = null!;
        public static ClinicDashboardRoutes ClinicDashboard { get; private set; } = null!;
        public static AdminPaymentsRoutes AdminPayments { get; private set; } = null!;
        public static AdminAdsRoutes AdminAds { get; private set; } = null!;
        public static AdsRoutes Ads { get; private set; } = null!;
        public static NotificationsRoutes Notifications { get; private set; } = null!;
        public static RatingsRoutes Ratings { get; private set; } = null!;

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
            public string Profile => $"{BaseRoute}/profile";
            public string UpdateProfile => $"{BaseRoute}/profile/update";
            public string FcmToken => $"{BaseRoute}/fcm-token";
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

            public string Availability => $"{_baseRoute}/doctors/availability";
            public string AvailabilityWeek => $"{_baseRoute}/doctors/availability/week";
            public string AvailabilityById(Guid id) => $"{_baseRoute}/doctors/availability/{id}";

            /// <summary>Patient booking: generated slots for one date (dynamic slot duration per availability row).</summary>
            public string Slots(Guid clinicId, Guid doctorId) => $"{_baseRoute}/clinics/{clinicId}/doctors/{doctorId}/slots";

            /// <summary>Patient booking: create an appointment (validates submitted time against the row's live duration).</summary>
            public string BookAppointment => $"{_baseRoute}/appointments";
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
            public string Settings => $"{AdminBaseRoute}/settings";
            public string Details(Guid id) => $"{AdminBaseRoute}/{id}/details";
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
            public string CreateSubscription => $"{DashboardRoute}/subscriptions";
            public string RevokeSubscription(Guid id) => $"{DashboardRoute}/subscriptions/{id}/revoke";
        }

        public class AdminDashboardRoutes
        {
            private readonly string _baseRoute;
            public AdminDashboardRoutes(string baseRoute)
            {
                _baseRoute = $"{baseRoute}/admin/dashboard";
            }

            public string Stats => $"{_baseRoute}/stats";
            public string UrgentTickets => $"{_baseRoute}/urgent-tickets";
            public string Subscriptions => $"{_baseRoute}/subscriptions";
            public string Tickets => $"{_baseRoute}/tickets";
            public string UpdateTicketStatus(Guid id) => $"{_baseRoute}/tickets/{id}/status";
        }

        public class DoctorDashboardRoutes
        {
            private readonly string _baseRoute;
            public DoctorDashboardRoutes(string baseRoute)
            {
                _baseRoute = $"{baseRoute}/doctors";
            }

            public string Stats => $"{_baseRoute}/dashboard/stats";
            public string RecentAppointments(int limit = 5) => $"{_baseRoute}/dashboard/recent-appointments?limit={limit}";
            public string Appointments => $"{_baseRoute}/appointments";
            public string Status(Guid id) => $"{_baseRoute}/appointments/{id}/status";
            public string AcceptAppointment(Guid id) => $"{_baseRoute}/appointments/{id}/accept";
            public string RejectAppointment(Guid id) => $"{_baseRoute}/appointments/{id}/reject";
            public string CompleteAppointment(Guid id) => $"{_baseRoute}/appointments/{id}/complete";
            public string Patients => $"{_baseRoute}/patients";
            public string PatientHistory(Guid patientId) => $"{_baseRoute}/patients/{patientId}/history";
        }

        public class ClinicDashboardRoutes
        {
            private readonly string _baseRoute;
            public ClinicDashboardRoutes(string baseRoute)
            {
                _baseRoute = $"{baseRoute}/admin/clinics";
            }

            public string Stats => $"{_baseRoute}/dashboard/stats";
            public string Bookings => $"{_baseRoute}/bookings";
            public string AcceptBooking => $"{_baseRoute}/bookings/accept";
            public string RejectBooking => $"{_baseRoute}/bookings/reject";
        }

        public class AdminPaymentsRoutes
        {
            private readonly string _baseRoute;
            public AdminPaymentsRoutes(string baseRoute)
            {
                _baseRoute = $"{baseRoute}/admin/payments";
            }

            public string List => $"{_baseRoute}";
            public string Stats => $"{_baseRoute}/stats";
            public string Detail(Guid id) => $"{_baseRoute}/{id}";
            public string Manual => $"{_baseRoute}/manual";
            public string Refund(Guid id) => $"{_baseRoute}/{id}/refund";
        }

        public class AdminAdsRoutes
        {
            private readonly string _baseRoute;
            public AdminAdsRoutes(string baseRoute)
            {
                _baseRoute = $"{baseRoute}/admin/ads";
            }

            public string List => $"{_baseRoute}";
            public string EligibleClinics => $"{_baseRoute}/eligible-clinics";
            public string Packages => $"{_baseRoute}/packages";
            public string Package(Guid id) => $"{_baseRoute}/packages/{id}";
            public string Orders => $"{_baseRoute}/orders";
            public string Deactivate(Guid id) => $"{_baseRoute}/{id}/deactivate";
        }

        public class AdsRoutes
        {
            private readonly string _apiBase;
            private readonly string _baseRoute;
            public AdsRoutes(string baseRoute)
            {
                _apiBase = baseRoute;
                _baseRoute = $"{baseRoute}/ads";
            }

            public string MyAds(Guid clinicId) => $"{_apiBase}/clinics/{clinicId}/ads";
            public string CreateOrder(Guid clinicId) => $"{_apiBase}/clinics/{clinicId}/ads/orders";
            public string Packages => $"{_baseRoute}/packages";
            public string PublicActive => $"{_apiBase}/public/ads/active";
        }

        public class NotificationsRoutes
        {
            private readonly string _baseRoute;
            public NotificationsRoutes(string baseRoute)
            {
                _baseRoute = $"{baseRoute}/notifications";
            }

            public string Count => $"{_baseRoute}/count";
            public string List => $"{_baseRoute}/pagginated";
        }

        public class RatingsRoutes
        {
            private readonly string _apiBase;
            public string BaseRoute { get; }
            public RatingsRoutes(string baseRoute)
            {
                _apiBase = baseRoute;
                BaseRoute = $"{baseRoute}/ratings";
            }

            public string Create => BaseRoute;
            public string DoctorRatings(Guid doctorId) => $"{_apiBase}/doctors/{doctorId}/ratings";
            public string ClinicRatings(Guid clinicId) => $"{_apiBase}/clinics/{clinicId}/ratings";
            public string PlaceCleanlinessRatings(Guid clinicId) => $"{_apiBase}/clinics/{clinicId}/place-cleanliness-ratings";
        }
    }
}
