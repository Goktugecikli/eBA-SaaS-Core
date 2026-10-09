using SanShine.SaaS.Core.Infrastucture.Repositories;

namespace SanShine.Saas.Core.WebApi.Modules
{
    public static class DataAccessModule
    {
        public static void AddDataAccess(this IServiceCollection services)
        {
            services.AddTransient<IHealthRepository, HealthRepository>();
            services.AddTransient<IAuthenticationRepository, AuthenticationRepository>();
        }
    }
}
