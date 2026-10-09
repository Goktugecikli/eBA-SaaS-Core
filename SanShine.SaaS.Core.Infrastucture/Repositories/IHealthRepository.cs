using SanShine.SaaS.Core.Common.Constants;
using SanShine.SaaS.Core.Common.Core.Utilities;
using SanShine.SaaS.Core.Infrastucture.DbManagerRepository;
using SanShine.SaaS.Core.Infrastucture.Entities.Health;
using SanShine.SaaS.Core.Infrastucture.Queries;

namespace SanShine.SaaS.Core.Infrastucture.Repositories;

public interface IHealthRepository
{
    Task<HealthEntity> PingAsync();
}

public class HealthRepository : IHealthRepository
{
    public HealthRepository()
    {
    }

    public async Task<HealthEntity> PingAsync()
    {
        try
        {
            var result = (await DbManager.ReadAsync<HealthEntity>(HealthQueries.Ping, null)).FirstOrDefault();
            return result;
        }
        catch (Exception ex)
        {
            LogHelper.WriteToFileAsync($"[ERROR] Hata: {ex}", LogConstants.CommonFileName, LogConstants.FolderName);
            throw;
        }
    }
}
