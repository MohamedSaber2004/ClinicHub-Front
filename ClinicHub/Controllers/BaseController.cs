using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClinicHub.Data;

namespace ClinicHub.Controllers
{
    public class BaseController : Controller
    {
        public CurrentUserContext? CurrentUser { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.CurrentUser = CurrentUser;
            base.OnActionExecuting(context);
        }
    }
}
