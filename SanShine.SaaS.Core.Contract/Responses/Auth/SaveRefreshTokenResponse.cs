namespace SanShine.SaaS.Core.Contract.Responses.Auth;

public class LoginRefreshResponse : BaseControllerResponse
{
    public string Token { get; set; }

    public string RefreshToken { get; set; }
    public string AuthHelper { get; set; }

    public DateTime TokenExpireDate { get; set; }
}
