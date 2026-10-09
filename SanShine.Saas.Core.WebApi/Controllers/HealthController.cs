using Microsoft.AspNetCore.Mvc;
using SanShine.SaaS.Core.Application.Services;
using SanShine.SaaS.Core.Contract.Requests.Health;

namespace SanShine.Saas.Core.WebApi.Controllers
{
    [Route("api/rest/")]
    [ApiController]
    public class HealthController : CustomBaseController
    {
        private readonly IHealthService _healthService;

        public HealthController(IHealthService healthService)
        {
            _healthService = healthService;
        }

        [HttpPost("health-echo")]
        public async Task<IActionResult> HealthEcho([FromBody] HealthRequest request)
            => await HandleServiceResponse(() => _healthService.EchoAsync(request));
    }
}
