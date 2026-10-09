using Castle.DynamicProxy;
using Microsoft.Data.SqlClient;
using SanShine.SaaS.Core.Common.Core.Extensions.CustomExceptions;
using SanShine.SaaS.Core.Common.Core.Utilities;

namespace SanShine.SaaS.Core.Common.Core.Aspects.Autofac.ExceptionHandling;

public class DatabaseExceptionHandlingAspect : Attribute, IInterceptor
{

    public void Intercept(IInvocation invocation)
    {
        try
        {
            invocation.Proceed(); // Metodu çalıştır
        }
        catch (SqlException ex)
        {
            string methodName = invocation.Method.Name;
            string errorMessage = $"[DATABASE ERROR] {methodName}: {ex.Message}";

            // Loglama mekanizması
            LogHelper.WriteToFileAsync(errorMessage, "Error_Logs", "DB_Errors");

            throw new DatabaseException(errorMessage, ex.Number, methodName, ex);
        }
        catch (Exception ex)
        {
            string methodName = invocation.Method.Name;
            string errorMessage = $"[UNKNOWN DATABASE ERROR] {methodName}: {ex.Message}";

            LogHelper.WriteToFileAsync(errorMessage, "Error_Logs", "DB_Errors");

            throw new DatabaseException(errorMessage, innerException: ex);
        }
    }
}
