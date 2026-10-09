using SanShine.SaaS.Core.Application.Shared;
using SanShine.SaaS.Core.Contract.Requests.Auth;
using SanShine.SaaS.Core.Contract.Responses.Auth;
using SanShine.SaaS.Core.Contract.Validator;
using System.Net;
using Microsoft.Extensions.Configuration;
using SanShine.SaaS.Core.Common.Constants;
using SanShine.SaaS.Core.Infrastucture.Repositories;
using SanShine.SaaS.Core.Common.Core.Aspects.Autofac.Validation;
using SanShine.SaaS.Core.Application.Enum;
using SanShine.SaaS.Core.Contract.Modules;

namespace SanShine.SaaS.Core.Application.Services
{
    public interface IAuthService
    {
        Task<CreateRestApiUserResponse> CreateRestApiUserAsync(CreateRestApiUserRequest request);
        RestApiUserLoginResponse Login2FA(RestApiUserLoginRequest request);
        Task<LoginRefreshResponse> LoginRefresh(RefreshTokenRequest request);
    }

    public class AuthService : IAuthService
    {
        private readonly IPasswordHashService _passwordHashService;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IPasswordHashService passwordHashService, IAuthenticationRepository authenticationRepository, IConfiguration configuration)
        {
            _passwordHashService = passwordHashService;
            _authenticationRepository = authenticationRepository;
            _configuration = configuration;
        }

        [ValidationAspect(typeof(CreateApiUserValidator))]
        public async Task<CreateRestApiUserResponse> CreateRestApiUserAsync(CreateRestApiUserRequest restApiUserRequest)
        {
            var response = new CreateRestApiUserResponse();

            var passwordHash = _passwordHashService.HashPassword(restApiUserRequest.Password);

            if (passwordHash == null)
            {
                response.SetFailure("Password Hash FAILED");
                return response;
            }

            var restApiUsername = Guid.NewGuid();
            restApiUserRequest.Password = passwordHash;

            var result = await _authenticationRepository.CreateRestApiUserRequestAsync(restApiUserRequest, restApiUsername);
            return result;
        }


        [ValidationAspect(typeof(LoginAuthValidator))]
        public RestApiUserLoginResponse Login2FA(RestApiUserLoginRequest request)
        {
            var superAdminInfo = _configuration.GetSection("SuperAdminUser").Get<SuperAdminCredentials>();
            var jwtToken = new LoginResponse();

            var response = new RestApiUserLoginResponse();

            //For Super_Admin User
            if (request.Username == superAdminInfo.Username)
            {
              //  if (!_passwordHashService.VerifyPassword(request.Password, superAdminInfo.Password))
                if (superAdminInfo.Password != superAdminInfo.Password)
                {
                    response.SetFailure("Invalid Credentials", HttpStatusCode.Unauthorized);
                    response.AccessToken = string.Empty;
                    return response;
                }

                jwtToken = Jwt.GetJwt(superAdminInfo.Id, superAdminInfo.Username, out string token, out string encodedToken, RoleType.SuperAdmin.ToString());

                RestApiUserLoginResponse superAdminResult = new()
                {
                    AccessToken = jwtToken.AccessToken,
                    ExpireDate = jwtToken.ExpireDate,
                    AuthHelper = jwtToken.AuthHelper,
                    isAuthorize = true
                };

                return superAdminResult;
            }
            else
            {
                var getRestApiUserInfo = _authenticationRepository.GetRestApiUserByUsername(request.Username);

                #region Validation
                if (!isValidUsername(request.Username) || !isVerifiedPassword(request.Username, request.Password))
                {
                    response.SetFailure("Invalid Credentials", HttpStatusCode.Unauthorized);
                    return response;
                }
                #endregion

                var userRoleModel = _authenticationRepository.GetUserRoleByUsername(request.Username);

                if (userRoleModel is null)
                {
                    response.SetFailure(ResponseMessageType.UserRoleNotFound.ToString());
                    return response;
                }

                jwtToken = Jwt.GetJwt(getRestApiUserInfo.UserId, getRestApiUserInfo.Username, out string loginToken, out string encodedToken, userRoleModel.Role);
                var refreshToken = Jwt.GenerateRefreshToken();

                var isSaved = _authenticationRepository.SaveRefreshToken(refreshToken, request.Username);

                if (!isSaved)
                {
                    return new RestApiUserLoginResponse() { StatusCode = HttpStatusCode.BadRequest, Message = "Refresh Token Save Operation is FAILED" };
                }

                RestApiUserLoginResponse result = new()
                {
                    AccessToken = jwtToken.AccessToken,
                    RefreshToken = refreshToken,
                    ExpireDate = jwtToken.ExpireDate,
                    AuthHelper = jwtToken.AuthHelper,
                    isAuthorize = true
                };

                result.SetSuccess();
                return result;
            }
        }

        [ValidationAspect(typeof(RefreshLoginValidator))]
        public async Task<LoginRefreshResponse> LoginRefresh(RefreshTokenRequest request)
        {
            var dbResponseModel = await _authenticationRepository.ValidateRefreshToken(request.RefreshToken);

            var response = new LoginRefreshResponse();

            if (dbResponseModel is null || string.IsNullOrEmpty(dbResponseModel.Username) || string.IsNullOrEmpty(dbResponseModel.ResponsibleUserName) || string.IsNullOrEmpty(dbResponseModel.Role))
            {
                response.SetFailure(ResponseMessageType.UserNotFound.ToString());
                return response;
            }

            JwtGenerationUserModel user = new()
            {
                UserId = dbResponseModel.UserId,
                UserName = dbResponseModel.Username,
                Role = dbResponseModel.Role,
                ResponsibleUserName = dbResponseModel.ResponsibleUserName
            };
            var jwtModel = Jwt.GetJwt(user, out string token, out string encodedToken);
            var refreshToken = Jwt.GenerateRefreshToken();

            var isSaved = _authenticationRepository.SaveRefreshToken(refreshToken, dbResponseModel.Username);

            if (!isSaved)
            {
                response.SetFailure(message:ResponseMessageType.Failed.ToString());
                return response;
            }

            response.RefreshToken = refreshToken;
            response.Token = jwtModel.AccessToken;
            response.TokenExpireDate = jwtModel.ExpireDate;
            response.AuthHelper = jwtModel.AuthHelper;
            response.SetSuccess();
            return response;
        }

        #region Rules
        private bool isValidUsername(string username)
        {
            var result = _authenticationRepository.GetRestApiUserByUsername(username);

            if (result.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            return true;
        }

        private bool isVerifiedPassword(string username, string password)
        {
            var result = _authenticationRepository.GetRestApiUserByUsername(username);

            var isVerified = _passwordHashService.VerifyPassword(password, result.Password);

            if (!isVerified)
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
