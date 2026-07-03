using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
