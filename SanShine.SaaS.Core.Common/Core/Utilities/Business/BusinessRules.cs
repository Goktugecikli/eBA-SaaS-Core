using SanShine.SaaS.Core.Common.Core.Utilities.Results;

namespace SanShine.SaaS.Core.Common.Core.Utilities.Business;

public class BusinessRules
{
    public static IResult Run(params IResult[] logics)
    {
        foreach (var logic in logics)
        {
            if (!logic.Success)
            {
                return logic;
            }
        }
        return null;
    }
}
