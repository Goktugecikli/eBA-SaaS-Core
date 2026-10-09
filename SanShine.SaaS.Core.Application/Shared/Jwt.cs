using Microsoft.IdentityModel.Tokens;
using SanShine.SaaS.Core.Common.Constants;
using SanShine.SaaS.Core.Contract.Modules;
using SanShine.SaaS.Core.Contract.Responses.Auth;
using SanShine.SaaS.Core.Contract.Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SanShine.SaaS.Core.Application.Shared
{
    public static class Jwt
    {
        public static LoginResponse GetJwt(JwtGenerationUserModel userModel, out string loginToken, out string refreshToken)
        {

            loginToken = string.Empty;
            refreshToken = string.Empty;
            var now = DateTime.Now;

            var claims = new Claim[]
            {
                new (JwtTokenClaims.UserId, userModel.UserId.ToString()),
                new (JwtTokenClaims.Username, userModel.UserName.ToString()),
                new (ClaimTypes.Role, userModel.Role),
                new(JwtTokenClaims.ResponsibleUser,userModel.ResponsibleUserName)
            };

            var symmetricKeyAsBase64 = JwtRestApi.SignKey;
            var keyByteArray = Encoding.UTF8.GetBytes(symmetricKeyAsBase64);

            //login token
            var signingKey = new SymmetricSecurityKey(keyByteArray);
            var jwt = new JwtSecurityToken(
                issuer: JwtRestApi.ValidIssuer,
                audience: JwtRestApi.ValidAudience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(JwtRestApi.TokenExpiration),
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
            
            var encodedRefreshJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            loginToken = encodedJwt;
            refreshToken = encodedRefreshJwt;

            var response = new LoginResponse()
            {
                AccessToken = encodedJwt,
                RefreshToken = encodedRefreshJwt,
                AuthHelper = $"Bearer {encodedJwt}",
                CreatedAt = now,
                Username = userModel.UserName,
                ExpireDate = now.AddMinutes(JwtRestApi.TokenExpiration)
            };

            return response;
        }
        public static LoginResponse GetJwt(int userId, string username, out string loginToken, out string refreshToken, string userRole = "Member")
        {

            loginToken = string.Empty;
            refreshToken = string.Empty;
            var now = DateTime.Now;

            var claims = new Claim[]
            {
                new (JwtTokenClaims.UserId, userId.ToString()),
                new (JwtTokenClaims.Username, userId.ToString()),
                new (ClaimTypes.Role, userRole)
            };

            var symmetricKeyAsBase64 = JwtRestApi.SignKey;
            var keyByteArray = Encoding.UTF8.GetBytes(symmetricKeyAsBase64);

            //login token
            var signingKey = new SymmetricSecurityKey(keyByteArray);
            var jwt = new JwtSecurityToken(
                issuer: JwtRestApi.ValidIssuer,
                audience: JwtRestApi.ValidAudience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(JwtRestApi.TokenExpiration),
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
            
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

    
            var encodedRefreshJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            loginToken = encodedJwt;
            refreshToken = encodedRefreshJwt;

            var response = new LoginResponse()
            {
                AccessToken = encodedJwt,
                RefreshToken = encodedRefreshJwt,
                AuthHelper = $"Bearer {encodedJwt}",
                CreatedAt = now,
                Username = username,
                ExpireDate = now.AddMinutes(JwtRestApi.TokenExpiration)
            };

            return response;
        }


        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
