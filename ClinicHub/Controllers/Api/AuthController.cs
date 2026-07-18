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
            var ctx = new CurrentUserContext
            {
                Id = 6,
                Role = UserRole.ClinicOwner,
                Permissions = RolePermissions.For(UserRole.ClinicOwner)
            };

            var mockUsers = new List<(int Id, string Name, string Email, string Phone)>
            {
                (1, "محمد عمر", "mohamed@email.com", "+966 50 111 2222"),
                (6, "أحمد المدير", "ahmed@clinic1.com", "+966 55 111 2222"),
            };
            var user = mockUsers.FirstOrDefault(u => u.Id == ctx.Id);

            var permissions = new List<string>();
            foreach (Permission p in Enum.GetValues<Permission>())
            {
                if (p != Permission.None && ctx.Has(p))
                    permissions.Add(p.ToString());
            }

            var response = new
            {
                id = ctx.Id,
                fullName = user.Name ?? "مستخدم",
                email = user.Email ?? "",
                phoneNumber = user.Phone ?? "",
                role = ctx.Role.ToString(),
                permissions,
            };

            return Ok(response);
        }
    }
}
