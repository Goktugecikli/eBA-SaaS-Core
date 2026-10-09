using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SanShine.SaaS.Core.Application.Services;
using SanShine.SaaS.Core.Contract.Requests.Auth;
using SanShine.SaaS.Core.Contract.Responses.Auth;

namespace SanShine.Saas.Core.WebApi.Controllers
{
    [Route("api/rest/auth/")]
    [ApiController]
    public class AuthController : CustomBaseController
    {
        private readonly IAuthService _authenticationService;

        public AuthController(IAuthService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("login-refresh")]
        public async Task<IActionResult> LoginRefreshAsync([FromBody]RefreshTokenRequest request)
        {
            return (await HandleServiceResponse(() => _authenticationService.LoginRefresh(request)));
        }

        [HttpPost("login2fa")]
        public IActionResult Login2FA(RestApiUserLoginRequest request)
        {
            var result =_authenticationService.Login2FA(new RestApiUserLoginRequest(){Username = request.Username, Password = request.Password});

            if (!result.isAuthorize)
            {
                return Unauthorized(result);
            }
            
            return Ok(result);
        }

        [Authorize(Roles=("SuperAdmin"))]
        [HttpPost("create-user")]
        public async Task<CreateRestApiUserResponse> CreateRestApiUser(CreateRestApiUserRequest createRestApiUserRequest)
        {

            var result = await _authenticationService.CreateRestApiUserAsync(createRestApiUserRequest);

            return result;
        }
    }
}
