using SanShine.SaaS.Core.Infrastucture.DbManagerRepository;
using SanShine.SaaS.Core.Infrastucture.Entities.BaseEntities;


namespace SanShine.SaaS.Core.Infrastucture.Entities;

public class RestApiUserInfoEntity : BaseEntity
{
    [DbManager.sColumn("username")]
    public string Username { get; set; }

    [DbManager.sColumn("status")]
    public char Status { get; set; }

    [DbManager.sColumn("company")]
    public string Company { get; set; }

    [DbManager.sColumn("branch")]
    public string Branch { get; set; }
}
