using Newtonsoft.Json;

namespace SanShine.SaaS.Core.Contract.Responses.Auth
{
    public class RestApiUserResponse : BaseControllerResponse
    {
        [JsonProperty("id")]
        public int UserId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
