using Microsoft.AspNetCore.Mvc;
using SanShine.SaaS.Core.Contract.Responses;

namespace SanShine.Saas.Core.WebApi.Controllers
{
    public abstract class CustomBaseController : ControllerBase
    {
        protected async Task<IActionResult> HandleServiceResponse<T>(Func<Task<T>> serviceMethod) where T : BaseControllerResponse
        {
            if (serviceMethod == null)
            {
                throw new ArgumentNullException(nameof(serviceMethod), "Service method cannot be null");
            }
            var response = await serviceMethod();
            if (response == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Response was null");
            }

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.OK => Ok(response),
                System.Net.HttpStatusCode.Created => Created(string.Empty, response),
                System.Net.HttpStatusCode.NoContent => NoContent(),
                System.Net.HttpStatusCode.BadRequest => BadRequest(response),
                System.Net.HttpStatusCode.Unauthorized => Unauthorized(response),
                System.Net.HttpStatusCode.Forbidden => Forbid(),
                System.Net.HttpStatusCode.NotFound => NotFound(response),
                System.Net.HttpStatusCode.Conflict => Conflict(response),
                _ => StatusCode((int)response.StatusCode, response),
            };
        }
    }
}
