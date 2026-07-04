using ClinicHub.Services.Contracts;
using ClinicHub.Services.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicHub.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
