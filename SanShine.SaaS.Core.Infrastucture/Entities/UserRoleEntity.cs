using SanShine.SaaS.Core.Infrastucture.DbManagerRepository;

namespace SanShine.SaaS.Core.Infrastucture.Entities;

public class UserRoleEntity
{
    [DbManager.sColumn("role")]
    public string Role { get; set; }
}
