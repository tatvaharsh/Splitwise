using Microsoft.AspNetCore.Mvc;
using SplitWise.Domain.DTO.Response;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/Activity")]
public class ActivityController(IActivityService activityService) : BaseController
{
    private readonly IActivityService _activityService = activityService;

    [HttpGet("GetList")]
    public async Task<IActionResult> GetList()
    {
        List<ActivityResponse> res = await _activityService.GetAllListQuery();
        return SuccessResponse(content: res);
    }
}
