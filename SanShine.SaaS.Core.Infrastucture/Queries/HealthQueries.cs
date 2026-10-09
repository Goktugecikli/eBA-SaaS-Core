namespace SanShine.SaaS.Core.Infrastucture.Queries;

internal static class HealthQueries
{
    internal const string Ping = @"SELECT 1 AS health_check";
}
