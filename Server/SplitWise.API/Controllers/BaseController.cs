using Microsoft.AspNetCore.Mvc;
using SplitWise.Domain;
using SplitWise.Domain.DTO.Response;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseController : ControllerBase
{
    protected IActionResult SuccessResponse<T>(int statusCode = StatusCodes.Status200OK, string message = SplitWiseConstants.SUCCESS_MESSAGE, T? content = null) where T : class
    {
        ApiResponse<T> response = new(statusCode, message, content);

        return new ObjectResult(response)
        {
            StatusCode = statusCode
        };
    }
}
