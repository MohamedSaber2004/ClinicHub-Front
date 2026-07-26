using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;

namespace ClinicHub.Controllers
{
    public class DoctorController : BaseController
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            CurrentUser = new CurrentUserContext
            {
                Id = 2,
                ClinicId = MockData.ClinicId_Heart,
                Role = UserRole.Doctor,
                Permissions = RolePermissions.For(UserRole.Doctor),
                PlanFeatures = PlanFeature.ManageAppointments | PlanFeature.ManagePatientRecords,
                HasActivePlan = true
            };
            base.OnActionExecuting(context);
        }

        public IActionResult Index()
        {
            ViewBag.Stats = MockData.GetDoctorDashboardStats();
            ViewBag.Appointments = MockData.GetDoctorAppointments().Take(5).ToList();
            return View();
        }

        public IActionResult Appointments()
        {
            ViewBag.Appointments = MockData.GetDoctorAppointments();
            return View();
        }

        public IActionResult Patients()
        {
            ViewBag.Patients = MockData.GetDoctorPatients();
            return View();
        }

        public IActionResult PatientHistory(int patientId)
        {
            ViewBag.PatientId = patientId;
            ViewBag.History = MockData.GetPatientHistory(patientId);
            ViewBag.PatientName = MockData.GetDoctorPatients()
                .FirstOrDefault(p => p.Id == patientId)?.Name ?? "مريض";
            return View();
        }
    }
}
