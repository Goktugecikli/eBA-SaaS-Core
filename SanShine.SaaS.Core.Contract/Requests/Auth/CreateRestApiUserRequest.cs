namespace SanShine.SaaS.Core.Contract.Requests.Auth;

public class CreateRestApiUserRequest
{
    public string Password { get; set; }

    public string CompanyId { get; set; }
    public string Company { get; set; }

    public string? BranchId { get; set; }
    public string Branch { get; set; }
    public string? Role { get; set; }
    public string ResponsibleUserName { get; set; }

}
