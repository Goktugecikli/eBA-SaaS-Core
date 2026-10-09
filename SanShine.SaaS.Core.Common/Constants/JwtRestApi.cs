namespace SanShine.SaaS.Core.Common.Constants
{
    public class JwtRestApi
    {
        public const string ValidLoginValue = "loggedin";
        public const string SignKey = "xEqwFekKmcmjaKqkwellKkwemdsjerjqQkqkzkPkkeMMbfdcX";
        public const string ValidAudience = "Sanko Sanshine";
        public const string ValidIssuer = "https://api.sanshine.com.tr/rest";

        public const int TokenExpiration = 60 * 24 * 1; // 60 minutes
        public const int RefreshTokenExpiration = 1 * 24 * 60; // 10 days
    }
}
