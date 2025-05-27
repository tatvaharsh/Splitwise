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
public class ExpenseController(IActivityService activityService, IExpenseService expenseService, IGroupService groupService, IActivityLoggerService activityLoggerService,
IFriendService friendService, IAppContextService appContextService, ITransactionService transactionService) : BaseController
{
    private readonly IActivityService _activityService = activityService;
    private readonly ITransactionService _transactionService = transactionService;
    private readonly IFriendService _friendService = friendService;
    private readonly IActivityLoggerService _activityLoggerService = activityLoggerService;
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
        // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);
        if (id == Guid.Empty)
            return BadRequest("Invalid Group ID");

        var activity = await _activityService.GetByIdAsync(id);
        var splits = await _activityService.GetOneAsync(x => x.Id == id, query => query.Include(x => x.ActivitySplits));

        string groupName = "";
        if (activity.Groupid.HasValue)
        {
            var group = await _groupService.GetByIdAsync(activity.Groupid.Value);
            groupName = group?.Groupname ?? "";
        }

        // Log for payer
        string payerMessage = activity.Groupid != null
            ? $"You deleted the expense '{activity.Description}' in group '{groupName}'"
            : $"You deleted the expense '{activity.Description}'";
        if (activity.Paidbyid.HasValue)
        {
            await _activityLoggerService.LogAsync(activity.Paidbyid.Value, payerMessage);
        }

        // Log for other participants (if group or individual split)
        foreach (var split in splits.ActivitySplits)
        {
            if (split.Userid != activity.Paidbyid)
            {
                string userMessage = activity.Groupid != null
                    ? $"The expense '{activity.Description}' was deleted in group '{groupName}'"
                    : $"The expense '{activity.Description}' was deleted";

                if (split.Userid.HasValue)
                {
                    await _activityLoggerService.LogAsync(split.Userid.Value, userMessage);
                }
            }
        }

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

    [HttpGet("settle-summary/{groupId}")]
    public async Task<IActionResult> GetSettleSummaryAsync([FromRoute] Guid groupId)
    {
        if (groupId == Guid.Empty)
            return BadRequest("Invalid group ID.");

        // Step 1: Get net balances from group activities (expenses, shares)
        var activityBalances = await _activityService.CalculateNetBalancesForGroupAsync(groupId);

        // Step 2: Fetch completed transactions for this group
        var completedTransactions = await _transactionService.GetListAsync
            (t => t.Groupid == groupId && t.Status == "completed" && !t.Isdeleted);

        // Step 3: Adjust net balances using transaction history
        foreach (var transaction in completedTransactions)
        {
            if (transaction.Payerid.HasValue && activityBalances.ContainsKey(transaction.Payerid.Value))
            {
                activityBalances[transaction.Payerid.Value] += transaction.Amount;
            }

            if (transaction.Receiverid.HasValue && activityBalances.ContainsKey(transaction.Receiverid.Value))
            {
                activityBalances[transaction.Receiverid.Value] -= transaction.Amount;
            }
        }
        //return for only logged in user
        var simplifiedSettlements = _activityService.CalculateMinimalSettlements(activityBalances);
        return SuccessResponse(content: simplifiedSettlements);
    }
}