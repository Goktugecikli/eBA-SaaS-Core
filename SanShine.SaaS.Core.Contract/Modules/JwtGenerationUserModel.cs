
namespace SanShine.SaaS.Core.Contract.Modules;
public class JwtGenerationUserModel
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public string Role { get; set; } = "Member";
    public string? ResponsibleUserName { get; set; }

}
