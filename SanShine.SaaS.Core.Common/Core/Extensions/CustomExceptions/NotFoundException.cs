namespace SanShine.SaaS.Core.Common.Core.Extensions.CustomExceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
