namespace SanShine.SaaS.Core.Common.Core.Extensions.CustomExceptions;

public class DatabaseException : Exception
{
    public int ErrorCode { get; }
    public string ProcedureName { get; }

    public DatabaseException(string message, int errorCode = 0, string procedureName = null, Exception innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ProcedureName = procedureName;
    }
}
