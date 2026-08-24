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
            services.AddMemoryCache();

            services.AddTransient<BearerTokenHandler>();
            services.AddTransient<ClinicHeaderHandler>();
            services.AddTransient<ApiLoggingHandler>();

            // Clean client (no auth handlers) used by BearerTokenHandler to rotate tokens.
            services.AddHttpClient("BearerTokenRefresh", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient<IAuthService, AuthService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<ISpecializationService, SpecializationService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IUserService, UserService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<ClinicHeaderHandler>();

            services.AddHttpClient<IAttachmentService, AttachmentService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IUserVerificationService, UserVerificationService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IClinicService, ClinicService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<ClinicHeaderHandler>();

            services.AddHttpClient<IDoctorService, DoctorService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddScoped<IAttachmentUrlResolver, AttachmentUrlResolver>();
            services.AddSingleton<IDeserializerService, DeserializerService>();

            services.AddHttpClient<IPlanService, PlanService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<ISubscriptionService, SubscriptionService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<ClinicHeaderHandler>();

            services.AddHttpClient<IAdminSubscriptionService, AdminSubscriptionService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IClinicDoctorService, ClinicDoctorService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<ClinicHeaderHandler>();

            services.AddHttpClient<IClinicStaffService, ClinicStaffService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<ClinicHeaderHandler>();

            services.AddHttpClient<IStaffDashboardService, StaffDashboardService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IAdminDashboardService, AdminDashboardService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IDoctorDashboardService, DoctorDashboardService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IClinicDashboardService, ClinicDashboardService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<ClinicHeaderHandler>();

            services.AddHttpClient<IAdminPaymentService, AdminPaymentService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IAdService, AdService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<ClinicHeaderHandler>();

            services.AddHttpClient<INotificationService, NotificationService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IRatingsService, RatingsService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IClinicPaymentService, ClinicPaymentService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient<IPlatformSettingService, PlatformSettingService>(client =>
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ar");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiLoggingHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();

            return services;
        }
    }
}
