using Microsoft.AspNetCore.Mvc;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Response;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/Activity")]
public class ActivityController(IActivityService activityService, IActivityLoggerService activityLoggerService, IAppContextService appContextService) : BaseController
{
    private readonly IActivityService _activityService = activityService;
    private readonly IAppContextService _appContextService = appContextService;
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
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

        List<ActivityLog> res = (await _activityLoggerService.GetListAsync(x => x.UserId == userId))
        .OrderByDescending(x => x.CreatedAt)
        .ToList();
        return SuccessResponse(content: res);
    }
}
