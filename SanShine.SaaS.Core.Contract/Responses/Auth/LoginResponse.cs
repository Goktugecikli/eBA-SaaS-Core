using Newtonsoft.Json;

namespace SanShine.SaaS.Core.Contract.Responses.Auth
{
    public class LoginResponse
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("authHelper")]
        public string AuthHelper { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("createdAt")]
        public DateTime ExpireDate { get; set; }
    }
}
