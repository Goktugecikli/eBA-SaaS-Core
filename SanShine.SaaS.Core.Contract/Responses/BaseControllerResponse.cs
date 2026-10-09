using System.Net;
using System.Text.Json.Serialization;

namespace SanShine.SaaS.Core.Contract.Responses
{
    public abstract class BaseControllerResponse
    {
        //public bool Success { get; set; } = false;
        public string Message { get; set; }
        [JsonIgnore]
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;

        public void SetSuccess(string message = "SUCCESS", HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            this.Message = message;
            StatusCode = httpStatusCode;
        }

        public void SetFailure(string message = "ERROR", HttpStatusCode httpStatusCode  = HttpStatusCode.BadRequest)
        {
            this.Message = message;
            StatusCode = httpStatusCode;
        }

        public T SetNoContent<T>(string message = "NoContent", HttpStatusCode statusCode = HttpStatusCode.NoContent) where T : BaseControllerResponse, new()
        {
            return new T
            {
                StatusCode = statusCode,
                Message = message
            };
        }
        public T SetNotFound<T>(string message = "NotFound", HttpStatusCode statusCode = HttpStatusCode.NotFound) where T : BaseControllerResponse, new()
        {
            return new T
            {
                StatusCode = statusCode,
                Message = message
            };
        }
        public T SetFailure<T>(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) where T : BaseControllerResponse, new()
        {
            return new T
            {
                StatusCode = statusCode,
                Message = message
            };
        }

        public T SetSuccess<T>(string? message = "SUCCESS", HttpStatusCode statusCode = HttpStatusCode.OK) where T : BaseControllerResponse, new() // Just Use Transaction Result 
        {
            return new T
            {
                StatusCode = statusCode,
                Message = message
            };
        }
        public T SetUnprocessableContent<T>(string message, HttpStatusCode statusCode = HttpStatusCode.UnprocessableEntity) where T : BaseControllerResponse, new()
        {
            return new T
            {
                StatusCode = statusCode,
                Message = message
            };
        }
    }
}
