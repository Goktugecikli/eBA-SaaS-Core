using SanShine.SaaS.Core.Infrastucture.DbManagerRepository;

namespace SanShine.SaaS.Core.Infrastucture.Entities.Health;

public class HealthEntity
{
    [DbManager.sColumn("health_check")]
    public int HealthCheck { get; set; }
}
