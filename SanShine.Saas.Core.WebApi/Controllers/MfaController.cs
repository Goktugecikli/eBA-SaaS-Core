using Microsoft.AspNetCore.Mvc;
using SanShine.SaaS.Core.Application.Services;

namespace SanShine.Saas.Core.WebApi.Controllers
{
	[Route("api/rest/mfa/")]
	[ApiController]
	public class MfaController : Controller
	{
		private readonly IMfaService _mfaService;

		public MfaController(IMfaService mfaService)
		{
			_mfaService = mfaService;
		}

		[HttpPost("get-secret-key")]
		public IActionResult GetSecretKey()
		{
			var secretKey = _mfaService.GenerateSecretKey();
			return Ok(new 
			{
				SecretKey = secretKey
			});
		}

		[HttpPost("generate-totp")]
		public IActionResult GenerateTOTP(string secretKey, int timeStep = 30)
		{
			var otp = _mfaService.GenerateTOTP(secretKey, timeStep);
			return Ok(new
			{
				SecretKey = secretKey,
				TimeStep = timeStep,
				Otp = otp
			});
		}

		[HttpPost("validate-totp")]
		public IActionResult ValidateTOTP(string secretKey, string code, int timeStep)
		{
			var validate = _mfaService.ValidateTOTP(secretKey, code, timeStep);
			return Ok(new
			{
				SecretKey = secretKey,
				Code = code,
				Validate = validate
			});
		}
	}
}
