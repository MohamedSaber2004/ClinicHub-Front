using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;

namespace ClinicHub.Controllers
{
    public class AdminController : BaseController
    {
        private readonly ISpecializationService _specializationService;
        private readonly IAttachmentUrlResolver _attachmentUrlResolver;
        private readonly IUserVerificationService _userVerificationService;

        public AdminController(ISpecializationService specializationService, IAttachmentUrlResolver attachmentUrlResolver, IUserVerificationService userVerificationService)
        {
            _specializationService = specializationService;
            _attachmentUrlResolver = attachmentUrlResolver;
            _userVerificationService = userVerificationService;
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

        public async Task<IActionResult> Specializations(int pageNumber = 1, int pageSize = 20, bool? isFamous = null)
        {
            try
            {
                var paged = await _specializationService.GetAllAsync(pageNumber, pageSize, isFamous);
                foreach (var s in paged.Items)
                {
                    if (!string.IsNullOrWhiteSpace(s.IconUrl) && !Uri.TryCreate(s.IconUrl, UriKind.Absolute, out _))
                    {
                        s.IconUrl = _attachmentUrlResolver.Resolve(s.IconUrl);
                    }
                }
                ViewBag.Specializations = paged.Items;
                ViewBag.Pagination = paged;
                ViewBag.CurrentFilter = isFamous;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Specializations = new List<SpecializationDto>();
            }
            return View();
        }

        [Route("Admin/Specializations/{id:guid}")]
        public async Task<IActionResult> SpecializationDetail(Guid id)
        {
            try
            {
                var response = await _specializationService.GetByIdAsync(id);
                var spec = response?.Data;
                if (spec == null) return RedirectToAction("Specializations");

                if (!string.IsNullOrWhiteSpace(spec.IconUrl) && !Uri.TryCreate(spec.IconUrl, UriKind.Absolute, out _))
                {
                    spec.IconUrl = _attachmentUrlResolver.Resolve(spec.IconUrl);
                }
                ViewBag.Specialization = spec;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSpecialization([FromForm] CreateSpecializationRequest request)
        {
            try
            {
                await _specializationService.CreateAsync(request);
                TempData["SuccessMessage"] = "تم إضافة التخصص بنجاح";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Specializations");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSpecialization(Guid id,[FromForm] UpdateSpecializationRequest request)
        {
            try
            {
                request.Id = id;
                await _specializationService.UpdateAsync(request);
                TempData["SuccessMessage"] = "تم تحديث التخصص بنجاح";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Specializations");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSpecialization(Guid id)
        {
            try
            {
                var msg = await _specializationService.DeleteAsync(new DeleteSpecializationRequest(id));
                TempData["SuccessMessage"] = msg;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Specializations");
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
        public async Task<IActionResult> VerificationCenter(int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var paged = await _userVerificationService.GetPendingVerificationsAsync(new GetPendingVerficationsRequest { PageNumber = pageNumber, PageSize = pageSize });
                ViewBag.Requests = paged;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Requests = new PagginatedResult<UserVerficationDto>(new List<UserVerficationDto>(), 0);
            }

            return View("VerificationCenter");
        }

        [Route("Admin/Subscriptions")]
        public IActionResult Subscriptions()
        {
            ViewBag.SubscriptionPlans = MockData.GetAllSubscriptionPlans();
            return View("Subscriptions");
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
