using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpDelete("delete/{id:Guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest("Invalid Group ID");
        return SuccessResponse<object>(message: await _activityService.DeleteAsync(id));
    }

    [HttpGet("getbyexpenseid/{id:Guid}")]
    public async Task<IActionResult> GetByExpenseId([FromRoute] Guid id)
    {
        var response = await _activityService.GetExpenseByIdAsync(id);
        return SuccessResponse(content: response);
    }

    [HttpGet("get/{id}")]
    public async Task<IActionResult> GetExpenseByGroupId([FromRoute] Guid id)
    {
        Guid currentUserId = Guid.Parse("78c89439-8cb5-4e93-8565-de9b7cf6c6ae");

        List<Activity> groupEntities = await _activityService.GetListAsync(
            x => x.Groupid == id && x.Paidbyid != null,
            query => query.Include(x => x.ActivitySplits).ThenInclude(x => x.User)
        ) ?? throw new Exception();

        decimal totalOweLent = 0; // Total net lent or owed

        var groupResponses = groupEntities
            .OrderByDescending(g => g.CreatedAt)
            .Select(groupEntity =>
            {
                decimal owelentAmount = 0;

                if (groupEntity.Paidbyid == currentUserId)
                {
                    // User paid, so they lent to others
                    owelentAmount = groupEntity.ActivitySplits.Sum(split => split.Splitamount)
                        - groupEntity.ActivitySplits.FirstOrDefault(split => split.Userid == currentUserId)?.Splitamount ?? 0;
                }
                else
                {
                    // User owes their part
                    owelentAmount = groupEntity.ActivitySplits
                        .FirstOrDefault(split => split.Userid == currentUserId)?.Splitamount ?? 0;
                    owelentAmount *= -1; // Owed amount is negative
                }

                totalOweLent += owelentAmount;

                return new GetExpenseByGroupId
                {
                    Id = groupEntity.Id,
                    Description = groupEntity.Description,
                    PayerName = groupEntity.Paidbyid == currentUserId
                        ? "You"
                        : groupEntity.ActivitySplits.FirstOrDefault(x => x.Userid == groupEntity.Paidbyid)?.User.Username,
                    Amount = groupEntity.Amount,
                    Date = groupEntity.Time,
                    OweLentAmount = Math.Abs(owelentAmount),
                    OweLentAmountOverall = 0 
                };
            }).ToList();

        // Update OweLentAmountOverall for each item (optional)
        foreach (var item in groupResponses)
        {
            item.OweLentAmountOverall = totalOweLent;
        }

        return SuccessResponse(content: groupResponses);
    }


}