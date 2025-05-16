using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.DTO.Response;
using SplitWise.Service;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/Expense")]
public class ExpenseController(IActivityService activityService, IExpenseService expenseService, IGroupService groupService,
IFriendService friendService, IAppContextService appContextService) : BaseController
{
    private readonly IActivityService _activityService = activityService;
    private readonly IFriendService _friendService = friendService;
    private readonly IGroupService _groupService = groupService;
    private readonly IExpenseService _expenseService = expenseService;
    private readonly IAppContextService _appContextService = appContextService;

    [HttpGet("list")]
    public async Task<IActionResult> GetList()
    {
        List<OnlyGroupResponse> groups = await _groupService.GetGroupsAsync() ?? [];
        List<MemberResponse> friends = await _friendService.GetFriendsAsync() ?? [];

        GetGroupsWithFriendsResponse response = new()
        {
            Groups = groups,
            Friends = friends
        };
        return SuccessResponse(content: response);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateActivityRequest request)
    {
        return SuccessResponse<object>(message: await _activityService.CreateActivityAsync(request));
    }

    [HttpPut("edit/{id:Guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateActivityRequest command)
    {
        command.Id = id;
        return SuccessResponse<object>(message: await _activityService.EditActivityAsync(command));
    }

}