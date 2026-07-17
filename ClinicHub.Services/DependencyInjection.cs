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

            services.AddTransient<BearerTokenHandler>();

            services.AddHttpClient<IAuthService, AuthService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
            })
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<ISpecializationService, SpecializationService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
            })
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IUserService, UserService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
            })
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IAttachmentService, AttachmentService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
            })
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IUserVerificationService, UserVerificationService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
            })
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddScoped<IAttachmentUrlResolver, AttachmentUrlResolver>();
            services.AddSingleton<IDeserializerService, DeserializerService>();

            return services;
        }
    }
}
