
namespace SanShine.SaaS.Core.Contract.Modules;

public interface IAppConfiguration
{
    Audience Audience { get; }

    int TokenExpiration { get; set; }

    int RefreshTokenExpiration { get; set; }
}

public class AppConfiguration : IAppConfiguration
{

    public Audience Audience { get; set; }

    public int TokenExpiration { get; set; }

    public int RefreshTokenExpiration { get; set; }
}

public class Audience
{
    public string Secret { get; set; }

    public string Iss { get; set; }

    public string Aud { get; set; }
}
