using SanShine.SaaS.Core.Common.Core.Aspects.Autofac.Validation;
using SanShine.SaaS.Core.Contract.Requests.Health;
using SanShine.SaaS.Core.Contract.Responses;
using SanShine.SaaS.Core.Contract.Validator;
using SanShine.SaaS.Core.Infrastucture.Repositories;

namespace SanShine.SaaS.Core.Application.Services
{
    public interface IHealthService
    {
        Task<HealthResponse> EchoAsync(HealthRequest request);
    }

    public class HealthService : IHealthService
    {
        private readonly IHealthRepository _healthRepository;

        public HealthService(IHealthRepository healthRepository)
            => _healthRepository = healthRepository;

        [ValidationAspect(typeof(HealthValidator))]
        public async Task<HealthResponse> EchoAsync(HealthRequest request)
        {
            var response = new HealthResponse
            {
                Echo = request.Message
            };

            try
            {
                var ping = await _healthRepository.PingAsync();
                response.DatabaseReachable = ping != null && ping.HealthCheck == 1;
            }
            catch
            {
                response.DatabaseReachable = false;
            }

            response.SetSuccess();
            return response;
        }
    }
}
