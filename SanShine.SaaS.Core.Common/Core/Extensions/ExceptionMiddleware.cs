using FluentValidation;
using Microsoft.AspNetCore.Http;
using SanShine.SaaS.Core.Common.Core.Extensions.CustomExceptions;
using SanShine.SaaS.Core.Common.Core.Utilities;

namespace SanShine.SaaS.Core.Common.Core.Extensions
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception e)
            {
                await HandleExceptionAsync(httpContext, e);
            }
        }

        private async Task HandleExceptionAsync(HttpContext httpContext, Exception e)
        {
            httpContext.Response.ContentType = "application/json";

            var pathSegments = httpContext.Request.Path.Value.Trim('/').Split('/');
            var servicePath = pathSegments.Length > 2 ? string.Join("/", pathSegments.Skip(2)) : "UnknownService";

            string responseMessage;
            string logFileName;
            string uniqueErrorId = Guid.NewGuid().ToString();

            string detailedLogMessage = $"[ERROR ID: {uniqueErrorId}] || {e}";
            if (e is NotFoundException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                string defaultText = "Verilen bilgilere göre sistemde veri bulunamadı ";
                responseMessage = string.IsNullOrEmpty(e.Message) ? defaultText : $"{defaultText} Hata: {e.Message}";
                logFileName = $"DbError_{servicePath}";
                await httpContext.Response.WriteAsync(new ErrorDetails
                {
                    StatusCode = httpContext.Response.StatusCode,
                    Message = e.Message,
                    ErrorId = uniqueErrorId
                }.ToString());
                return;
            }

            if (e is ValidationException validationException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

                await httpContext.Response.WriteAsync(new ValidationErrorDetails
                {
                    StatusCode = httpContext.Response.StatusCode,
                    Errors = validationException.Errors,
                    Message = validationException.Message,
                    ErrorId = uniqueErrorId
                }.ToString());
                return;
            }
            if (e is BusinessRulesException businessException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                responseMessage = businessException.Message;
                logFileName = $"BusinessError_{servicePath}";
            }
            else if (e is DatabaseException dbException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                string defaultText ="Veritabanı işlemi sırasında bir hata oluştu. Lütfen yetkili ile iletişime geçiniz";
                responseMessage = string.IsNullOrEmpty(e.Message) ? defaultText : $"{defaultText} Hata: {dbException.Message}";
                logFileName = $"DbError_{servicePath}";
            } 
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                responseMessage =  $"Internal Server Error.";
                logFileName = $"SystemError_{servicePath}";

            }
            await LogHelper.WriteToFileAsync($"{detailedLogMessage} : ", $"{logFileName}", $"SystemErrors/{servicePath.Replace('/','_')}");
            await httpContext.Response.WriteAsync(new ErrorDetails
            {
                StatusCode = httpContext.Response.StatusCode,
                ErrorId = uniqueErrorId,
                Message = responseMessage
            }.ToString());
         
         
        }


    }
}
