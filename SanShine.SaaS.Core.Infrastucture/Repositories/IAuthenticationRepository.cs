using SanShine.SaaS.Core.Contract.Requests.Auth;
using SanShine.SaaS.Core.Contract.Responses.Auth;
using SanShine.SaaS.Core.Infrastucture.DbManagerRepository;
using SanShine.SaaS.Core.Infrastucture.Entities;
using SanShine.SaaS.Core.Infrastucture.Queries;
using System.Net;

namespace SanShine.SaaS.Core.Infrastucture.Repositories
{
    public interface IAuthenticationRepository
    {
        Task<CreateRestApiUserResponse> CreateRestApiUserRequestAsync(CreateRestApiUserRequest request, Guid restApiUsername);
        RestApiUserResponse GetRestApiUserByUsername(string username);
        bool SaveRefreshToken(string refreshToken, string username);
        UserRoleEntity GetUserRoleByUsername(string username);
        Task<RefreshTokenEntity> ValidateRefreshToken(string refreshToken);
    }

    public class AuthenticationRepository : IAuthenticationRepository
    {
        public async Task<CreateRestApiUserResponse> CreateRestApiUserRequestAsync(CreateRestApiUserRequest request, Guid restApiUsername)
        {
            var response = new CreateRestApiUserResponse();

            if(string.IsNullOrEmpty(request.Role))
            {
                request.Role = request.CompanyId + (string.IsNullOrEmpty(request.BranchId) ? "" : $"-{request.BranchId}");
            }

            try
            {
                var result = (await DbManager.ReadAsync<CreateRestApiEntity>(AuthenticationQueries.CreateRestApi, new Dictionary<string, object>
                {
                    {"@username", restApiUsername},
                    {"@company_id", request.CompanyId},
                    {"@company", request.Company.ToUpperInvariant()},
                    {"@branch", string.IsNullOrWhiteSpace(request.Branch) ? DBNull.Value : request.Branch.ToUpperInvariant()},
                    {"@branch_id", request.BranchId},
                    {"@password", request.Password},
                    {"@responsible_user_name", request.ResponsibleUserName},
                    {"@role", request.Role},
                    {"@status", "A"},
                    {"@created_by", -1} // -1 is system user
                })).FirstOrDefault();

                if (result == null)
                {
                     response.SetFailure("Not Implemented Rest API User", HttpStatusCode.NotFound);
                    return response;
                }

                response.Data = new CreateRestApiUserDataResponse()
                {
                    Id = result.Id,
                    Username = result.Username,
                    Status = result.Status,
                };
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public RestApiUserResponse GetRestApiUserByUsername(string username)
        {
            var dbResult = DbManager.Read<RestApiUserEntity>(AuthenticationQueries.GetRestApiUser, new Dictionary<string, object>
                {
                    {"@username", username}
                }).FirstOrDefault();

            if (dbResult == null)
            {
                return new RestApiUserResponse
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "Not Found Rest API User"
                };
            }

            return new RestApiUserResponse
            {
                StatusCode = HttpStatusCode.OK,
                UserId = dbResult.UserId,
                Username = dbResult.Username,
                Password = dbResult.Password
            };
        }

        public bool SaveRefreshToken(string refreshToken, string username)
        {
            var apiUser = DbManager.ExecuteNonQuery(AuthenticationQueries.SaveRefreshToken, new Dictionary<string, object>
            {
                { "@username", username },
                { "@refresh_token", refreshToken }
            });

            return apiUser;
        }
        
        public UserRoleEntity GetUserRoleByUsername(string username)
        {
            var dbResult = DbManager.Read<UserRoleEntity>(AuthenticationQueries.GetUserRoleByUsername, new Dictionary<string, object>
            {
                {"@username", username}
            }).FirstOrDefault();

            return dbResult;
        }

        public async Task<RefreshTokenEntity> ValidateRefreshToken(string refreshToken)
        {
            var refreshTokenInfo = (await DbManager.ReadAsync<RefreshTokenEntity>(AuthenticationQueries.GetUserByRefreshToken, new Dictionary<string, object>
            {
                { "@refresh_token", refreshToken }
            })).FirstOrDefault();

            return refreshTokenInfo;
        }
    }
}
