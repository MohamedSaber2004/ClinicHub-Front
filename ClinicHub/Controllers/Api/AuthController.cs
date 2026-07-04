using ClinicHub.Data;
using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult Me()
        {
            var ctx = CurrentUserContext.Current ?? new CurrentUserContext
            {
                Id = 6,
                Role = UserRole.ClinicOwner,
                Permissions = RolePermissions.For(UserRole.ClinicOwner)
            };

            var user = MockData.GetUsers().FirstOrDefault(u => u.Id == ctx.Id);

            var permissions = new List<string>();
            foreach (Permission p in Enum.GetValues<Permission>())
            {
                if (p != Permission.None && ctx.Has(p))
                    permissions.Add(p.ToString());
            }

            var response = new
            {
                id = ctx.Id,
                fullName = user?.Name ?? "مستخدم",
                email = user?.Email ?? "",
                phoneNumber = user?.Phone ?? "",
                role = ctx.Role.ToString(),
                permissions,
            };

            return Ok(response);
        }
    }
}
