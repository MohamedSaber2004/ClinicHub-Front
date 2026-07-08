using ClinicHub.Services.Contracts;
using ClinicHub.Services.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicHub.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISpecializationService, SpecializationService>();
            services.AddScoped<IAttachmentService, AttachmentService>();
            services.AddScoped<IAttachmentUrlResolver, AttachmentUrlResolver>();

            return services;
        }
    }
}
