namespace ClinicHub.Routes
{
    public static class AdminRoutes
    {
        public static string Base => "/Admin";

        public static class Pages
        {
            public static string Index() => $"{Base}/Index";
            public static string Specializations() => $"{Base}/Specializations";
            public static string Clinics() => $"{Base}/Clinics";
            public static string Doctors() => $"{Base}/Doctors";
            public static string Payments() => $"{Base}/Payments";
            public static string PaymentsDetails(Guid id) => $"{Base}/PaymentsDetails/{IdProtector.Protect(id)}";
            public static string PaymentsDetails(string token) => $"{Base}/PaymentsDetails/{token}";
            public static string Users() => $"{Base}/Users";
            public static string UsersOverview(Guid id) => $"{Base}/Users/Overview/{IdProtector.Protect(id)}";
            public static string UsersOverview(string token) => $"{Base}/Users/Overview/{token}";
            public static string UsersVisits(Guid id) => $"{Base}/Users/Visits/{IdProtector.Protect(id)}";
            public static string UsersVisits(string token) => $"{Base}/Users/Visits/{token}";
            public static string UsersRequests(Guid id) => $"{Base}/Users/Requests/{IdProtector.Protect(id)}";
            public static string UsersRequests(string token) => $"{Base}/Users/Requests/{token}";
            public static string UsersPayments(Guid id) => $"{Base}/Users/Payments/{IdProtector.Protect(id)}";
            public static string UsersPayments(string token) => $"{Base}/Users/Payments/{token}";
            public static string ClinicDetails(Guid id) => $"{Base}/Clinics/Details/{IdProtector.Protect(id)}";
            public static string ClinicDetails(string token) => $"{Base}/Clinics/Details/{token}";
            public static string DoctorDetails(Guid id) => $"{Base}/Doctors/Details/{IdProtector.Protect(id)}";
            public static string DoctorDetails(string token) => $"{Base}/Doctors/Details/{token}";
            public static string VerificationCenter() => $"{Base}/Verification";
            public static string Subscriptions() => $"{Base}/Subscriptions";
            public static string Profile() => $"{Base}/Profile";
            public static string PendingClinics() => $"{Base}/PendingClinics";
            public static string PlanManagement() => $"{Base}/PlanManagement";
            public static string SubscriptionManagement() => $"{Base}/SubscriptionManagement";
            public static string Ads() => $"{Base}/Ads";
            public static string Notifications() => $"{Base}/Notifications";
        }
    }
}
