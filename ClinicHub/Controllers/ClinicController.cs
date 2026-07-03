using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.Controllers
{
    public class ClinicController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
