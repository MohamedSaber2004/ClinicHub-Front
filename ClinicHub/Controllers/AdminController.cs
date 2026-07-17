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
        private readonly IUserService _userService;

        public AdminController(ISpecializationService specializationService, IAttachmentUrlResolver attachmentUrlResolver, IUserVerificationService userVerificationService, IUserService userService)
        {
            _specializationService = specializationService;
            _attachmentUrlResolver = attachmentUrlResolver;
            _userVerificationService = userVerificationService;
            _userService = userService;
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

        [HttpPost]
        public async Task<IActionResult> AcceptVerification(Guid userId)
        {
            try
            {
                var result = await _userVerificationService.ApproveUserVerificationAsync(new ApproveUserVerficationRequest { UserId = userId });
                if (result.Success)
                    TempData["SuccessMessage"] = "تم قبول طلب التحقق بنجاح";
                else
                    TempData["ErrorMessage"] = result.Message;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("VerificationCenter");
        }

        [HttpPost]
        public async Task<IActionResult> RejectVerification(Guid userId, string? notes)
        {
            try
            {
                var result = await _userVerificationService.RejectUserVerificationAsync(new RejectUserVerificationRequest { UserId = userId, Notes = notes });
                if (result.Success)
                    TempData["SuccessMessage"] = "تم رفض طلب التحقق";
                else
                    TempData["ErrorMessage"] = result.Message;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("VerificationCenter");
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
        public async Task<IActionResult> Users(int pageNumber = 1, int pageSize = 20, string? searchTerm = null)
        {
            try
            {
                var request = new GetAllUsersRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm
                };
                var paged = await _userService.GetAllUsersPagginatedAsync(request);

                var users = paged.Items.Select(u => new MockUser
                {
                    Id = u.Id.GetHashCode(),
                    Name = u.FullName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Initials = GetInitials(u.FullName),
                    RegistrationDate = u.CreatedAt.ToString("d MMMM yyyy"),
                    Status = u.IsActive ? "نشط" : "غير نشط",
                    StatusClass = u.IsActive ? "badge-success" : "badge-warning",
                    Role = MapUserTypeToRole(u.Roles.FirstOrDefault()),
                    TotalVisits = 0,
                    AvgRating = 0,
                    TotalSpent = "0"
                }).ToList();

                ViewBag.Users = users;
                ViewBag.Pagination = paged;
            }
            catch (ApiException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.Users = new List<MockUser>();
            }

            return View("Users/Index");
        }

        private static string GetInitials(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "";
            var first = parts[0][0].ToString();
            var second = parts.Length > 1 && parts[1].Length > 1 ? parts[1][1].ToString() : "";
            return first + second;
        }

        private static UserRole MapUserTypeToRole(ClinicHub.Services.Enums.UserType? userType)
        {
            return userType switch
            {
                ClinicHub.Services.Enums.UserType.SuperAdmin => UserRole.SystemAdmin,
                ClinicHub.Services.Enums.UserType.ClinicOwner => UserRole.ClinicOwner,
                ClinicHub.Services.Enums.UserType.Doctor => UserRole.Doctor,
                ClinicHub.Services.Enums.UserType.Staff => UserRole.ClinicStaff,
                ClinicHub.Services.Enums.UserType.User => UserRole.Patient,
                _ => UserRole.Patient
            };
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
