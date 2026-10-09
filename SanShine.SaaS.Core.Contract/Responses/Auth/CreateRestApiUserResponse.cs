using Newtonsoft.Json;

namespace SanShine.SaaS.Core.Contract.Responses.Auth
{
    public class CreateRestApiUserResponse : BaseControllerResponse
    {
        public CreateRestApiUserDataResponse Data { get; set; }
    }

    public class CreateRestApiUserDataResponse 
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }
 
        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
