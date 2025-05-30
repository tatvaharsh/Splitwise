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
IFriendService friendService, IUserService userService, IAppContextService appContextService, ITransactionService transactionService) : BaseController
{
    private readonly IActivityService _activityService = activityService;
    private readonly IUserService _userService = userService;
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

        // Fetch activity data
        List<Activity> groupEntities = await _activityService.GetListAsync(
            x => x.Groupid == id && x.Paidbyid != null,
            query => query.Include(x => x.ActivitySplits).ThenInclude(x => x.User)
        ) ?? throw new Exception();

        // Fetch settle-up transactions in this group
        List<Transaction> groupTransactions = await _transactionService.GetListAsync(
            x => x.Groupid == id
        );

        decimal totalOweLent = 0;

        // Expense entries
        var groupResponses = groupEntities
            .OrderByDescending(g => g.CreatedAt)
            .Select(groupEntity =>
            {
                decimal owelentAmount = 0;

                if (groupEntity.Paidbyid == currentUserId)
                {
                    owelentAmount = groupEntity.ActivitySplits.Sum(split => split.Splitamount)
                        - groupEntity.ActivitySplits.FirstOrDefault(split => split.Userid == currentUserId)?.Splitamount ?? 0;
                }
                else
                {
                    owelentAmount = groupEntity.ActivitySplits
                        .FirstOrDefault(split => split.Userid == currentUserId)?.Splitamount ?? 0;
                    owelentAmount *= -1;
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

        // Apply transaction adjustments
        foreach (var txn in groupTransactions)
        {
            if (txn.Payerid == currentUserId)
            {
                // If you owe money, paying reduces your debt
                if (totalOweLent < 0)
                    totalOweLent += txn.Amount;
                else
                    totalOweLent -= txn.Amount;
            }
            else if (txn.Receiverid == currentUserId)
            {
                // If you receive money, your debt is reduced, or your lend is reimbursed
                if (totalOweLent < 0)
                    totalOweLent += txn.Amount;
                else
                    totalOweLent -= txn.Amount;
            }
        }


        // Update overall balances
        foreach (var item in groupResponses)
        {
            item.OweLentAmountOverall = totalOweLent;
        }

        return SuccessResponse(content: groupResponses);
    }


    // [HttpGet("settle-summary/{groupId}")]
    // public async Task<IActionResult> GetSettleSummaryAsync([FromRoute] Guid groupId)
    // {
    //     if (groupId == Guid.Empty)
    //         return BadRequest("Invalid group ID.");

    //     // Step 1: Get net balances from group activities (expenses, shares)
    //     var activityBalances = await _activityService.CalculateNetBalancesForGroupAsync(groupId);

    //     var allTransactions = await _transactionService.GetListAsync(
    //         t => (t.Groupid == groupId || t.Groupid == null) && !t.Isdeleted
    //     );

    //     // Step 3: Filter in-memory using activityBalances
    //     var completedTransactions = allTransactions
    //         .Where(t =>
    //             (t.Payerid.HasValue && activityBalances.ContainsKey(t.Payerid.Value)) ||
    //             (t.Receiverid.HasValue && activityBalances.ContainsKey(t.Receiverid.Value)))
    //         .ToList();


    //     // Step 3: Adjust net balances using transaction history
    //     foreach (var transaction in completedTransactions)
    //     {
    //         if (transaction.Payerid.HasValue && activityBalances.ContainsKey(transaction.Payerid.Value))
    //         {
    //             activityBalances[transaction.Payerid.Value] += transaction.Amount;
    //         }

    //         if (transaction.Receiverid.HasValue && activityBalances.ContainsKey(transaction.Receiverid.Value))
    //         {
    //             activityBalances[transaction.Receiverid.Value] -= transaction.Amount;
    //         }
    //     }
    //     //return for only logged in user
    //     var simplifiedSettlements = _activityService.CalculateMinimalSettlements(activityBalances);
    //     return SuccessResponse(content: simplifiedSettlements);
    // }

    [HttpGet("settle-summary/{groupId}")]
    public async Task<IActionResult> GetSettleSummaryAsync([FromRoute] Guid groupId)
    {
        if (groupId == Guid.Empty)
            return BadRequest("Invalid group ID.");

        // Step 1: Get net balances from group activities (expenses, shares)
        var activityBalances = await _activityService.CalculateNetBalancesForGroupAsync(groupId);

        // Step 2: Get the group with its members
        var group = await _groupService.GetOneAsync(
            g => g.Id == groupId,
            query => query.Include(x => x.GroupMembers)
        );

        if (group == null)
            return NotFound("Group not found.");

        var groupMemberIds = group.GroupMembers
            .Where(m => !m.Isdeleted)
            .Select(m => m.Memberid)
            .ToHashSet();

        // Step 3: Fetch all transactions: both group and friend level
        var allTransactions = await _transactionService.GetListAsync(
            t => (t.Groupid == groupId || t.Groupid == null) && !t.Isdeleted
        );

        // Step 4: Filter relevant transactions
        var relevantTransactions = allTransactions
            .Where(t =>
                t.Groupid == groupId || // group transaction
                (t.Groupid != null &&
                 t.Payerid.HasValue &&
                 t.Receiverid.HasValue &&
                 groupMemberIds.Contains(t.Payerid.Value) &&
                 groupMemberIds.Contains(t.Receiverid.Value))
            )
            .ToList();

        // Step 5: Adjust balances using relevant transactions
        foreach (var transaction in relevantTransactions)
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

        // Step 6: Keep only balances for group members
        var groupOnlyBalances = activityBalances
            .Where(kvp => groupMemberIds.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Step 7: Calculate minimal settlements
        var simplifiedSettlements = _activityService.CalculateMinimalSettlements(groupOnlyBalances);

        return SuccessResponse(content: simplifiedSettlements);
    }




    [HttpPost("settle-up")]
    public async Task<IActionResult> SettleUp([FromBody] SettleUpRequest request)
    {
        if (request.Amount <= 0 || request.PayerId == Guid.Empty || request.ReceiverId == Guid.Empty)
            return BadRequest("Invalid input.");

        var result = await _activityService.SettleUpAsync(request);
        return SuccessResponse(content: result);
    }

    // [HttpGet("settle-summary/friends/{friend2Id}")] 
    // public async Task<IActionResult> GetFriendSettleSummaryAsync(
    //     [FromRoute] Guid friend2Id) 
    // {
    //     // Retrieve the logged-in user's ID
    //     var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
    //     Guid loggedInUserId = Guid.Parse(userIdString);
    //    // Guid? loggedInUserId = GetCurrentUserId();

    //     if (loggedInUserId == Guid.Empty)
    //     {
    //         return Unauthorized("User is not logged in or user ID is invalid.");
    //     }

    //     Guid friend1Id = loggedInUserId; // Assign logged-in user's ID to friend1Id

    //     // Validate input IDs
    //     if (friend2Id == Guid.Empty || friend1Id == friend2Id)
    //     {
    //         return BadRequest("Invalid friend ID or attempting to settle with self.");
    //     }

    //     // Step 1: Calculate net balances specifically between these two friends
    //     // This method will only consider expenses and splits relevant to friend1 and friend2
    //     var activityBalances = await _activityService.CalculateNetBalancesForFriendsAsync(friend1Id, friend2Id);

    //     // Step 2: Fetch completed transactions *only between these two friends*
    //     var completedTransactions = await _transactionService.GetListAsync(
    //         t => ((t.Payerid == friend1Id && t.Receiverid == friend2Id) ||
    //               (t.Payerid == friend2Id && t.Receiverid == friend1Id)) &&
    //              !t.Isdeleted);

    //     // Step 3: Adjust net balances using the transaction history between them
    //     foreach (var transaction in completedTransactions)
    //     {
    //         if (transaction.Payerid.HasValue && activityBalances.ContainsKey(transaction.Payerid.Value))
    //         {
    //             activityBalances[transaction.Payerid.Value] += transaction.Amount;
    //         }

    //         if (transaction.Receiverid.HasValue && activityBalances.ContainsKey(transaction.Receiverid.Value))
    //         {
    //             activityBalances[transaction.Receiverid.Value] -= transaction.Amount;
    //         }
    //     }

    //     // Step 4: Calculate minimal settlements using the adjusted balances
    //     // This will return who owes whom and how much, simplified to minimal transactions
    //     var simplifiedSettlements = _activityService.CalculateMinimalSettlements(activityBalances);

    //     return SuccessResponse(content: simplifiedSettlements);
    // }

    // [HttpGet("settle-summary/friends/{friend2Id}")] 
    // public async Task<IActionResult> GetFriendSettleSummaryAsync(
    //     [FromRoute] Guid friend2Id) 
    // {
    //     // Retrieve the logged-in user's ID
    //     var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
    //     Guid loggedInUserId = Guid.Parse(userIdString);
    //    // Guid? loggedInUserId = GetCurrentUserId();

    //     if (loggedInUserId == Guid.Empty)
    //     {
    //         return Unauthorized("User is not logged in or user ID is invalid.");
    //     }

    //     Guid friend1Id = loggedInUserId; // Assign logged-in user's ID to friend1Id

    //     // Validate input IDs
    //     if (friend2Id == Guid.Empty || friend1Id == friend2Id)
    //     {
    //         return BadRequest("Invalid friend ID or attempting to settle with self.");
    //     }

    //     // Step 1: Calculate net balances specifically between these two friends
    //     // This method will only consider expenses and splits relevant to friend1 and friend2
    //     var activityBalances = await _activityService.CalculateNetBalancesForFriendsAsync(friend1Id, friend2Id);

    //     // Step 2: Fetch completed transactions *only between these two friends*
    //     var completedTransactions = await _transactionService.GetListAsync(
    //         t => ((t.Payerid == friend1Id && t.Receiverid == friend2Id) ||
    //               (t.Payerid == friend2Id && t.Receiverid == friend1Id)) &&
    //              !t.Isdeleted);

    //     // Step 3: Adjust net balances using the transaction history between them
    //     foreach (var transaction in completedTransactions)
    //     {
    //         if (transaction.Payerid.HasValue && activityBalances.ContainsKey(transaction.Payerid.Value))
    //         {
    //             activityBalances[transaction.Payerid.Value] += transaction.Amount;
    //         }

    //         if (transaction.Receiverid.HasValue && activityBalances.ContainsKey(transaction.Receiverid.Value))
    //         {
    //             activityBalances[transaction.Receiverid.Value] -= transaction.Amount;
    //         }
    //     }

    //     // Step 4: Calculate minimal settlements using the adjusted balances
    //     // This will return who owes whom and how much, simplified to minimal transactions
    //     var simplifiedSettlements = _activityService.CalculateMinimalSettlements(activityBalances);

    //     return SuccessResponse(content: simplifiedSettlements);
    // }

    [HttpGet("settle-summary/friends/{friend2Id}")]
    public async Task<IActionResult> GetFriendSettleSummaryAsync(
    [FromRoute] Guid friend2Id)
    {
        // Retrieve the logged-in user's ID
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid loggedInUserId = Guid.Parse(userIdString);
        // Guid? loggedInUserId = GetCurrentUserId(); // Use this if you have a proper method to get user ID

        if (loggedInUserId == Guid.Empty)
        {
            return Unauthorized("User is not logged in or user ID is invalid.");
        }

        Guid friend1Id = loggedInUserId;

        // Validate input IDs
        if (friend2Id == Guid.Empty || friend1Id == friend2Id)
        {
            return BadRequest("Invalid friend ID or attempting to settle with self.");
        }
        var friendBalancesSummary = await _activityService.CalculateNetBalancesForFriendsAsync(friend1Id, friend2Id);

        // Get the separate balance dictionaries
        var oneToOneActivityBalances = friendBalancesSummary.OneToOneBalances;

        var completedTransactions = await _transactionService.GetListAsync(
            t => ((t.Payerid == friend1Id && t.Receiverid == friend2Id) ||
                  (t.Payerid == friend2Id && t.Receiverid == friend1Id)) &&
                  !t.Isdeleted);

        foreach (var transaction in completedTransactions)
        {
            if (transaction.Groupid == null)
            {
                if (transaction.Payerid.HasValue && oneToOneActivityBalances.ContainsKey(transaction.Payerid.Value))
                {
                    oneToOneActivityBalances[transaction.Payerid.Value] += transaction.Amount;
                }
                if (transaction.Receiverid.HasValue && oneToOneActivityBalances.ContainsKey(transaction.Receiverid.Value))
                {
                    oneToOneActivityBalances[transaction.Receiverid.Value] -= transaction.Amount;
                }
            }
            else
            {
                // Group transaction - adjust balances in the correct group balance dictionary
                var groupId = transaction.Groupid.Value;

                if (friendBalancesSummary.GroupBalancesPerGroup.TryGetValue(groupId, out var groupBalances))
                {
                    if (transaction.Payerid.HasValue && groupBalances.ContainsKey(transaction.Payerid.Value))
                        groupBalances[transaction.Payerid.Value] += transaction.Amount;

                    if (transaction.Receiverid.HasValue && groupBalances.ContainsKey(transaction.Receiverid.Value))
                        groupBalances[transaction.Receiverid.Value] -= transaction.Amount;
                }
            }

        }

        // Calculate settlements for each group
        var allGroupSettlements = new List<SettleSummaryDto>();
        foreach (var entry in friendBalancesSummary.GroupBalancesPerGroup)
        {
            var groupId = entry.Key;
            var groupBalances = entry.Value;
            var simplifiedSettlementsForGroup = _activityService.CalculateMinimalSettlement(groupBalances, groupId);
            allGroupSettlements.AddRange(simplifiedSettlementsForGroup);
        }

        // Calculate one-to-one settlements
        var simplifiedOneToOneSettlements = _activityService.CalculateMinimalSettlements(oneToOneActivityBalances);

        var finalSummary = new FriendSettlementSummaryDto
        {
            GroupSettlements = allGroupSettlements,
            OneToOneSettlements = simplifiedOneToOneSettlements
        };

        return SuccessResponse(content: finalSummary);
    }



}