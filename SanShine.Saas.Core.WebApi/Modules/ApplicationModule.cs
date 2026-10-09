using SanShine.SaaS.Core.Application.Services;

namespace SanShine.Saas.Core.WebApi.Modules
{
    public static class ApplicationModule
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddTransient<IPasswordHashService, PasswordHashService>();
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IHealthService, HealthService>();
            services.AddTransient<IMfaService, MfaService>();
            services.AddTransient<IPriceApprovalService, PriceApprovalService>();
            services.AddTransient<IWasteTrackingService, WasteTrackingService>();
        }
    }
}
