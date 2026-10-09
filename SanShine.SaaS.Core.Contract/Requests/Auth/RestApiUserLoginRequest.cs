using Newtonsoft.Json;

namespace SanShine.SaaS.Core.Contract.Requests.Auth
{
    public class RestApiUserLoginRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
