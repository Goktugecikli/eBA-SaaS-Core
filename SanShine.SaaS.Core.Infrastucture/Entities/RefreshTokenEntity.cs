using SanShine.SaaS.Core.Infrastucture.DbManagerRepository;

namespace SanShine.SaaS.Core.Infrastucture.Entities;

public class RefreshTokenEntity
{

    [DbManager.sColumn("id")]
    public int UserId { get; set; }
    [DbManager.sColumn("username")]
    public string Username { get; set; }

    [DbManager.sColumn("role")]
    public string Role { get; set; }
    [DbManager.sColumn("responsible_user_name")]
    public string ResponsibleUserName { get; set; }

    [DbManager.sColumn("refresh_token")]
    public string RefreshToken { get; set; }
}
