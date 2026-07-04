using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;

namespace ClinicHub.Controllers
{
    public class AdminController : BaseController
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            CurrentUser = new CurrentUserContext
            {
                Id = 0,
                Role = UserRole.SystemAdmin,
                Permissions = RolePermissions.For(UserRole.SystemAdmin)
            };
            base.OnActionExecuting(context);
        }

        public IActionResult Index()
        {
            ViewBag.Stats = MockData.GetDashboardStats();
            ViewBag.QuickStatuses = MockData.GetQuickStatuses();
            ViewBag.DoctorsOnDuty = MockData.GetDoctorsOnDuty();
            ViewBag.UrgentTickets = MockData.GetUrgentTickets();
            ViewBag.Subscribers = MockData.GetSubscribers();
            ViewBag.ActivityLog = MockData.GetActivityLog();
            ViewBag.NewPatients = MockData.GetNewPatients();
            return View();
        }

        public IActionResult Specializations()
        {
            ViewBag.Specializations = MockData.GetSpecializations();
            return View();
        }

        public IActionResult Clinics()
        {
            ViewBag.Clinics = MockData.GetClinics();
            return View();
        }

        [Route("Admin/Clinics/Details/{id}")]
        public IActionResult ClinicDetails(int id)
        {
            var clinic = MockData.GetClinicById(id);
            if (clinic == null) return RedirectToAction("Clinics");
            ViewBag.Clinic = clinic;
            ViewBag.Doctors = MockData.GetClinicDoctors(id);
            ViewBag.Staff = MockData.GetClinicStaff(id);
            ViewBag.Ratings = MockData.GetClinicRatings(id);
            ViewBag.AllClinics = MockData.GetClinics();
            return View("ClinicDetails");
        }

        public IActionResult Doctors()
        {
            ViewBag.Doctors = MockData.GetDoctors();
            ViewBag.Clinics = MockData.GetClinics();
            return View();
        }

        [Route("Admin/Doctors/Details/{id}")]
        public IActionResult DoctorDetails(int id)
        {
            ViewBag.Doctor = MockData.GetDoctorById(id);
            ViewBag.Clinics = MockData.GetClinics();
            return View("DoctorDetails");
        }

        [Route("Admin/Verification")]
        public IActionResult VerificationCenter()
        {
            ViewBag.Requests = MockData.GetPendingVerifications();
            return View("VerificationCenter");
        }

        [Route("Admin/Subscriptions")]
        public IActionResult Subscriptions()
        {
            ViewBag.SubscriptionPlans = MockData.GetAllSubscriptionPlans();
            return View("Subscriptions");
        }

        [Route("Admin/PendingClinics")]
        public IActionResult PendingClinics()
        {
            ViewBag.Registrations = MockData.GetAllClinicRegistrations();
            ViewBag.PendingCount = MockData.GetPendingClinicRegistrationsCount();
            return View("PendingClinics");
        }

        public IActionResult Support()
        {
            ViewBag.Tickets = MockData.GetSupportTickets();
            return View();
        }

        [Route("Admin/Ads")]
        public IActionResult Ads()
        {
            ViewBag.Stats = MockData.GetAdStats();
            ViewBag.Ads = MockData.GetAds();
            return View("Ads/Index");
        }

        [Route("Admin/Ads/Details/{id}")]
        public IActionResult AdsDetails(int id)
        {
            ViewBag.Detail = MockData.GetAdDetail(id);
            return View("Ads/Details");
        }

        public IActionResult Payments()
        {
            ViewBag.Stats = MockData.GetPaymentStats();
            ViewBag.Payments = MockData.GetPayments();
            return View();
        }

        public IActionResult PaymentsDetails(int id)
        {
            ViewBag.Detail = MockData.GetPaymentDetail(id);
            return View("PaymentsDetails");
        }

        [Route("Admin/Users")]
        public IActionResult Users()
        {
            ViewBag.Users = MockData.GetUsers();
            return View("Users/Index");
        }

        [Route("Admin/Users/Overview/{id}")]
        public IActionResult UsersOverview(int id)
        {
            ViewBag.User = MockData.GetUserOverview(id);
            return View("Users/Overview");
        }

        [Route("Admin/Users/Visits/{id}")]
        public IActionResult UsersVisits(int id)
        {
            ViewBag.UserId = id;
            ViewBag.Visits = MockData.GetUserVisits(id);
            return View("Users/Visits");
        }

        [Route("Admin/Users/Requests/{id}")]
        public IActionResult UsersRequests(int id)
        {
            ViewBag.UserId = id;
            ViewBag.Requests = MockData.GetUserRequests(id);
            return View("Users/Requests");
        }

        [Route("Admin/Users/Payments/{id}")]
        public IActionResult UsersPayments(int id)
        {
            ViewBag.UserId = id;
            ViewBag.Payments = MockData.GetUserPayments(id);
            return View("Users/Payments");
        }

        public IActionResult Profile()
        {
            return View();
        }
    }
}
