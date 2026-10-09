using SanShine.SaaS.Core.Infrastucture.DbManagerRepository;

namespace SanShine.SaaS.Core.Infrastucture.Entities;

public class RestApiUserEntity
{
    [DbManager.sColumn("id")]
    public int UserId { get; set; }

    [DbManager.sColumn("username")]
    public string Username { get; set; }

    [DbManager.sColumn("password")]
    public string Password { get; set; }
}
