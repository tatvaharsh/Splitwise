using Microsoft.AspNetCore.Mvc;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Response;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/Activity")]
public class ActivityController(IActivityService activityService, IActivityLoggerService activityLoggerService) : BaseController
{
    private readonly IActivityService _activityService = activityService;
    private readonly IActivityLoggerService _activityLoggerService = activityLoggerService;

    [HttpGet("GetList")]
    public async Task<IActionResult> GetList()
    {
        List<ActivityResponse> res = await _activityService.GetAllListQuery();
        return SuccessResponse(content: res);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetAllActivity()
    {
        // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);

        List<ActivityLog> res = (await _activityLoggerService.GetListAsync(x => x.UserId == userId))
        .OrderByDescending(x => x.CreatedAt)
        .ToList();
        return SuccessResponse(content: res);
    }
}
