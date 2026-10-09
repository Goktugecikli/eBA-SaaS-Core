

namespace SanShine.SaaS.Core.Contract.Responses.Auth;

public class RestApiUserLoginResponse : BaseControllerResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }

    public DateTime ExpireDate { get; set; }

    public string AuthHelper { get; set; }

    public bool isAuthorize { get; set; }
}
