using Microsoft.AspNetCore.Mvc;
using ClinicHub.Data;

namespace ClinicHub.Controllers
{
    public class AdminController : Controller
    {
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
            return View();
        }

        public IActionResult Clinics()
        {
            return View();
        }

        public IActionResult Doctors()
        {
            return View();
        }

        public IActionResult Support()
        {
            return View();
        }

        public IActionResult Ads()
        {
            return View();
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

        public IActionResult Profile()
        {
            return View();
        }
    }
}
