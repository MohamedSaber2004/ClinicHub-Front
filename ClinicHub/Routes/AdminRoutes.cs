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
            public static string Support() => $"{Base}/Support";
            public static string Payments() => $"{Base}/Payments";
            public static string PaymentsDetails(int id) => $"{Base}/PaymentsDetails/{id}";
            public static string Users() => $"{Base}/Users";
            public static string UsersOverview(int id) => $"{Base}/Users/Overview/{id}";
            public static string UsersVisits(int id) => $"{Base}/Users/Visits/{id}";
            public static string UsersRequests(int id) => $"{Base}/Users/Requests/{id}";
            public static string UsersPayments(int id) => $"{Base}/Users/Payments/{id}";
            public static string ClinicDetails(Guid id) => $"{Base}/Clinics/Details/{id}";
            public static string DoctorDetails(Guid id) => $"{Base}/Doctors/Details/{id}";
            public static string VerificationCenter() => $"{Base}/Verification";
            public static string Subscriptions() => $"{Base}/Subscriptions";
            public static string Profile() => $"{Base}/Profile";
            public static string PendingClinics() => $"{Base}/PendingClinics";
            public static string PlanManagement() => $"{Base}/PlanManagement";
            public static string SubscriptionManagement() => $"{Base}/SubscriptionManagement";
        }
    }
}
