using Microsoft.AspNetCore.Mvc;

namespace SanShine.Saas.Core.WebApi.Controllers
{
    [Route("api/rest/SKY/")]
    [ApiController]
    public class TestController : Controller
    {
       [HttpGet("get-environment")]
        public IActionResult GetEnvironment()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            return Ok(environment);
        }
    }
}
