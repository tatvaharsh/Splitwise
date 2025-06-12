using System.Text;
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
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

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

     [HttpGet("get/{id}")]
    public async Task<IActionResult> GetExpenseByGroupId([FromRoute] Guid id)
    {
        Guid currentUserId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 1. Fetch activity (expense) data, including activity splits and associated user details
        List<Activity> groupExpenses = await _activityService.GetListAsync(
            x => x.Groupid == id && x.Paidbyid != null,
            query => query.Include(x => x.ActivitySplits).ThenInclude(x => x.User)
        ) ?? throw new Exception("Could not fetch group expenses.");

        // 2. Fetch settle-up transaction data, including payer and receiver user details
        List<Transaction> groupTransactions = await _transactionService.GetListAsync(
            x => x.Groupid == id,
            query => query.Include(x => x.Payer).Include(x => x.Receiver)
        ) ?? throw new Exception("Could not fetch group transactions.");

        // List to hold all combined expense and transaction items
        List<GroupItemResponse> allGroupItems = new List<GroupItemResponse>();

        // 3. Process Expense entries and map to GroupItemResponse
        var expenseResponses = groupExpenses
            .Select(groupEntity =>
            {
                decimal owelentAmount = 0;

                // Calculate the individual owe/lent amount for the current user for this expense
                if (groupEntity.Paidbyid == currentUserId)
                {
                    // If current user paid, they lent the total amount minus their own share
                    owelentAmount = (groupEntity.Amount ?? 0) - (groupEntity.ActivitySplits.FirstOrDefault(split => split.Userid == currentUserId)?.Splitamount ?? 0);
                }
                else
                {
                    // If current user didn't pay, they owe their share (represented as negative)
                    owelentAmount = (groupEntity.ActivitySplits.FirstOrDefault(split => split.Userid == currentUserId)?.Splitamount ?? 0);
                    owelentAmount *= -1;
                }

                return new GroupItemResponse
                {
                    Id = groupEntity.Id,
                    Type = "Expense",
                    Description = groupEntity.Description,
                    PayerName = groupEntity.Paidbyid == currentUserId
                        ? "You"
                        : groupEntity.ActivitySplits.FirstOrDefault(x => x.Userid == groupEntity.Paidbyid)?.User.Username,
                    ReceiverName = null, // Not applicable for expenses
                    Amount = groupEntity.Amount ?? 0, // Use null-coalescing to handle potential null values
                    Date = groupEntity.Time ?? DateTime.UtcNow, // Use expense time for sorting
                    OweLentAmount = owelentAmount, // This is the impact of this specific expense
                    OweLentAmountOverall = 0 ,
                    OrderDate = groupEntity.CreatedAt ?? DateTime.UtcNow // Use expense time for sorting 
                };
            }).ToList();

        allGroupItems.AddRange(expenseResponses);

        // 4. Process Settle-Up Transactions and map to GroupItemResponse
        var transactionResponses = groupTransactions
            .Select(txn =>
            {
                decimal owelentAmount = 0;

                // Calculate the individual owe/lent amount for the current user for this transaction
                if (txn.Payerid == currentUserId)
                {
                    // Current user paid in a settle-up. This reduces their "owed" amount or increases their "lent" amount.
                    // Represents a positive impact on their net balance.
                    owelentAmount = txn.Amount;
                }
                else if (txn.Receiverid == currentUserId)
                {
                    // Current user received in a settle-up. This increases their "owed" amount or reduces their "lent" amount.
                    // Represents a negative impact on their net balance.
                    owelentAmount = -txn.Amount;
                }
                // If neither payer nor receiver is current user, owelentAmount remains 0, which is correct.

                string payerName = txn.Payerid == currentUserId ? "You" : txn.Payer?.Username;
                string receiverName = txn.Receiverid == currentUserId ? "you" : txn.Receiver?.Username; // "you" (lowercase) for better sentence flow

                return new GroupItemResponse
                {
                    Id = txn.Id,
                    Type = "SettleUp",
                    Description = $"{payerName} settled {receiverName}", // Descriptive text for settle-up
                    PayerName = payerName,
                    ReceiverName = receiverName,
                    Amount = txn.Amount,
                    Date = txn.Time ?? DateTime.UtcNow, // Use transaction time for sorting
                    OweLentAmount = owelentAmount, // This is the impact of this specific transaction
                    OweLentAmountOverall = 0 ,
                    OrderDate = txn.CreatedAt ?? DateTime.UtcNow // Use transaction time for sorting 
                };
            }).ToList();

        allGroupItems.AddRange(transactionResponses);

        // 5. Sort the combined list by Date (most recent first)
        allGroupItems = allGroupItems.OrderByDescending(item => item.OrderDate).ToList();

        // 6. Calculate the final overall owe/lent balance for the current user
        // This sum correctly accounts for all expenses and settle-up transactions.
        decimal totalOweLentOverall = allGroupItems.Sum(item => item.OweLentAmount);

        // 7. Update the OweLentAmountOverall for all items in the list to reflect the final balance
        foreach (var item in allGroupItems)
        {
            item.OweLentAmountOverall = totalOweLentOverall;
        }

        return SuccessResponse(content: allGroupItems);
    }

    [HttpGet("getbyexpenseid/{id:Guid}")]
    public async Task<IActionResult> GetByExpenseId([FromRoute] Guid id)
    {
        var response = await _activityService.GetExpenseByIdAsync(id);
        return SuccessResponse(content: response);
    }

    // [HttpGet("get/{id}")]
    // public async Task<IActionResult> GetExpenseByGroupId([FromRoute] Guid id)
    // {
    //     Guid currentUserId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

    //     // Fetch activity data
    //     List<Activity> groupEntities = await _activityService.GetListAsync(
    //         x => x.Groupid == id && x.Paidbyid != null,
    //         query => query.Include(x => x.ActivitySplits).ThenInclude(x => x.User)
    //     ) ?? throw new Exception();

    //     // Fetch settle-up transactions in this group
    //     List<Transaction> groupTransactions = await _transactionService.GetListAsync(
    //         x => x.Groupid == id
    //     );

    //     decimal totalOweLent = 0;

    //     // Expense entries
    //     var groupResponses = groupEntities
    //         .OrderByDescending(g => g.CreatedAt)
    //         .Select(groupEntity =>
    //         {
    //             decimal owelentAmount = 0;

    //             if (groupEntity.Paidbyid == currentUserId)
    //             {
    //                 owelentAmount = groupEntity.ActivitySplits.Sum(split => split.Splitamount)
    //                     - groupEntity.ActivitySplits.FirstOrDefault(split => split.Userid == currentUserId)?.Splitamount ?? 0;
    //             }
    //             else
    //             {
    //                 owelentAmount = groupEntity.ActivitySplits
    //                     .FirstOrDefault(split => split.Userid == currentUserId)?.Splitamount ?? 0;
    //                 owelentAmount *= -1;
    //             }

    //             totalOweLent += owelentAmount;

    //             return new GetExpenseByGroupId
    //             {
    //                 Id = groupEntity.Id,
    //                 Description = groupEntity.Description,
    //                 PayerName = groupEntity.Paidbyid == currentUserId
    //                     ? "You"
    //                     : groupEntity.ActivitySplits.FirstOrDefault(x => x.Userid == groupEntity.Paidbyid)?.User.Username,
    //                 Amount = groupEntity.Amount,
    //                 Date = groupEntity.Time,
    //                 OweLentAmount = Math.Abs(owelentAmount),
    //                 OweLentAmountOverall = 0
    //             };
    //         }).ToList();

    //     // Apply transaction adjustments
    //     foreach (var txn in groupTransactions)
    //     {
    //         if (txn.Payerid == currentUserId)
    //         {
    //             // If you owe money, paying reduces your debt
    //             if (totalOweLent < 0)
    //                 totalOweLent += txn.Amount;
    //             else
    //                 totalOweLent -= txn.Amount;
    //         }
    //         else if (txn.Receiverid == currentUserId)
    //         {
    //             // If you receive money, your debt is reduced, or your lend is reimbursed
    //             if (totalOweLent < 0)
    //                 totalOweLent += txn.Amount;
    //             else
    //                 totalOweLent -= txn.Amount;
    //         }
    //     }


    //     // Update overall balances
    //     foreach (var item in groupResponses)
    //     {
    //         item.OweLentAmountOverall = totalOweLent;
    //     }

    //     return SuccessResponse(content: groupResponses);
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

    [HttpGet("settle-summary/friends/{friend2Id}")]
    public async Task<IActionResult> GetFriendSettleSummaryAsync(
    [FromRoute] Guid friend2Id)
    {
        Guid loggedInUserId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
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

    [HttpGet("settle-summary/friends/{friend2Id}/transparency")] 
    public async Task<IActionResult> GetFriendSettleTransparencyAsync(
        [FromRoute] Guid friend2Id)
    {
        Guid loggedInUserId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        if (loggedInUserId == Guid.Empty)
        {
            return Unauthorized("User is not logged in or user ID is invalid.");
        }

        Guid friend1Id = loggedInUserId;

        if (friend2Id == Guid.Empty || friend1Id == friend2Id)
        {
            return BadRequest("Invalid friend ID or attempting to get transparency with self.");
        }

        // --- Step 1: Get Initial Activity Balances ---
        var friendBalancesSummary = await _activityService.CalculateNetBalancesForFriendsAsync(friend1Id, friend2Id);

        // Deep copy initial balances to store their state before transaction application
        var initialOneToOneActivityBalancesRaw = friendBalancesSummary.OneToOneBalances
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value); // Create a copy
        var initialGroupBalancesRaw = friendBalancesSummary.GroupBalancesPerGroup
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDictionary(userKvp => userKvp.Key, userKvp => userKvp.Value)); // Deep copy groups

        // --- Step 2: Get Relevant Transactions ---
        var completedTransactions = await _transactionService.GetListAsync(
            t => ((t.Payerid == friend1Id && t.Receiverid == friend2Id) ||
                    (t.Payerid == friend2Id && t.Receiverid == friend1Id)) &&
                    !t.Isdeleted);

        // You'll need to fetch user and group names here or pass IDs to a service that does
        // For demonstration, placeholders are used. In a real app, use your UserService/GroupService.
        var userIdsInvolved = new List<Guid> { friend1Id, friend2Id };
        userIdsInvolved.AddRange(completedTransactions.Where(t => t.Payerid.HasValue).Select(t => t.Payerid.Value));
        userIdsInvolved.AddRange(completedTransactions.Where(t => t.Receiverid.HasValue).Select(t => t.Receiverid.Value));
        userIdsInvolved = userIdsInvolved.Distinct().ToList();

        var groupIdsInvolved = completedTransactions.Where(t => t.Groupid.HasValue).Select(t => t.Groupid.Value).Distinct().ToList();

        // Imagine you have services to get names:
        var userNames = (await _userService.GetListAsync(x => initialOneToOneActivityBalancesRaw.Keys.Contains(x.Id)))
                .ToDictionary(u => u.Id, u => u.Username);
        var groupNames = (await _groupService.GetListAsync(x => initialGroupBalancesRaw.Keys.Contains(x.Id)))
                        .ToDictionary(g => g.Id, g => g.Groupname);

        // Populate TransactionDetailDto with names
        var relevantTransactionDetails = completedTransactions.Select(t => new TransactionDetailDto
        {
            Id = t.Id,
            Amount = t.Amount,
            PayerId = t.Payerid,
            PayerName = t.Payerid.HasValue && userNames.ContainsKey(t.Payerid.Value) 
                ? userNames[t.Payerid.Value] 
                : "Unknown User",

            ReceiverId = t.Receiverid,
            ReceiverName = t.Receiverid.HasValue && userNames.ContainsKey(t.Receiverid.Value)
                ? userNames[t.Receiverid.Value]
                : "Unknown User",

            GroupId = t.Groupid,
            GroupName = t.Groupid.HasValue && groupNames.ContainsKey(t.Groupid.Value)
                ? groupNames[t.Groupid.Value]
                : "Unknown Group"

        }).ToList();


        // --- Step 3: Apply Transactions to Balances (Logic from original API) ---
        // Note: The `friendBalancesSummary` object contains mutable dictionaries.
        // Changes applied here will be reflected when calculating final settlements.
        foreach (var transaction in completedTransactions)
        {
            if (transaction.Groupid == null)
            {
                if (transaction.Payerid.HasValue && friendBalancesSummary.OneToOneBalances.ContainsKey(transaction.Payerid.Value))
                {
                    friendBalancesSummary.OneToOneBalances[transaction.Payerid.Value] += transaction.Amount;
                }
                if (transaction.Receiverid.HasValue && friendBalancesSummary.OneToOneBalances.ContainsKey(transaction.Receiverid.Value))
                {
                    friendBalancesSummary.OneToOneBalances[transaction.Receiverid.Value] -= transaction.Amount;
                }
            }
            else
            {
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

        // --- Step 4: Capture Final Balances After Transactions ---
        var finalOneToOneActivityBalancesRaw = friendBalancesSummary.OneToOneBalances;
        var finalGroupBalancesRaw = friendBalancesSummary.GroupBalancesPerGroup;


        // --- Step 5: Calculate Settlements ---
        var allGroupSettlements = new List<SettleSummaryDto>();
        foreach (var entry in finalGroupBalancesRaw)
        {
            var groupId = entry.Key;
            var groupBalances = entry.Value;
            var simplifiedSettlementsForGroup = _activityService.CalculateMinimalSettlement(groupBalances, groupId);
            allGroupSettlements.AddRange(simplifiedSettlementsForGroup);
        }

        var simplifiedOneToOneSettlements = _activityService.CalculateMinimalSettlements(finalOneToOneActivityBalancesRaw);

        // --- Step 6: Populate the Transparency DTO ---
        var transparencySummary = new FriendSettlementTransparencyDto
        {
            InitialOneToOneActivityBalances = initialOneToOneActivityBalancesRaw
                .Select(kvp => new UserBalanceDetailDto { UserId = kvp.Key, Balance = kvp.Value , UserName = userNames.GetValueOrDefault(kvp.Key, "Unknown") })
                .ToList(),
            InitialGroupBalances = initialGroupBalancesRaw
                .Select(entry => new GroupBalanceDetailDto
                {
                    GroupId = entry.Key,
                    GroupName = groupNames.GetValueOrDefault(entry.Key, "Unknown Group"),
                    Balances = entry.Value.Select(kvp => new UserBalanceDetailDto { UserId = kvp.Key, Balance = kvp.Value , UserName = userNames.GetValueOrDefault(kvp.Key, "Unknown") }).ToList()
                }).ToList(),
            RelevantTransactions = relevantTransactionDetails, // This already has names if you fetched them
            FinalOneToOneActivityBalances = finalOneToOneActivityBalancesRaw
                .Select(kvp => new UserBalanceDetailDto { UserId = kvp.Key, Balance = kvp.Value , UserName = userNames.GetValueOrDefault(kvp.Key, "Unknown") })
                .ToList(),
            FinalGroupBalances = finalGroupBalancesRaw
                .Select(entry => new GroupBalanceDetailDto
                {
                    GroupId = entry.Key,
                    GroupName = groupNames.GetValueOrDefault(entry.Key, "Unknown Group"),
                    Balances = entry.Value.Select(kvp => new UserBalanceDetailDto { UserId = kvp.Key, Balance = kvp.Value , UserName = userNames.GetValueOrDefault(kvp.Key, "Unknown") }).ToList()
                }).ToList(),
            CalculatedGroupSettlements = allGroupSettlements,
            CalculatedOneToOneSettlements = simplifiedOneToOneSettlements
        };

        return SuccessResponse(content: transparencySummary);
    }

    [HttpGet("settle-summary-explained/{groupId}")]
    public async Task<IActionResult> GetSettleSummaryExplainedAsync([FromRoute] Guid groupId)
    {
        if (groupId == Guid.Empty)
            return BadRequest("Invalid group ID.");

        // initialActivityBalances: Net balances purely from group activities/expenses,
        // calculated by _activityService.CalculateNetBalancesForGroupAsync.
        var initialActivityBalances = await _activityService.CalculateNetBalancesForGroupAsync(groupId);
        var initialDetailedDebtsMap = await GetDetailedGroupDebtsAsync(groupId);

        var group = await _groupService.GetOneAsync(
            g => g.Id == groupId,
            query => query.Include(x => x.GroupMembers).ThenInclude(gm => gm.Member)
        );

        if (group == null)
            return NotFound("Group not found.");

        var groupMemberIds = group.GroupMembers
            .Where(m => !m.Isdeleted)
            .Select(m => m.Memberid)
            .ToHashSet();

        var memberNames = group.GroupMembers
            .Where(m => !m.Isdeleted)
            .ToDictionary(m => m.Memberid, m => m.Member?.Username ?? m.Memberid.ToString());

        var allTransactions = await _transactionService.GetListAsync(
            t => !t.Isdeleted // Fetch all non-deleted transactions to filter later
        );

        var relevantTransactions = allTransactions
            .Where(t =>
                t.Groupid == groupId || // Transactions explicitly linked to this group
                (t.Groupid == null &&   // OR Transactions not linked to a group but involve two members of *this* group
                 t.Payerid.HasValue &&
                 t.Receiverid.HasValue &&
                 groupMemberIds.Contains(t.Payerid.Value) &&
                 groupMemberIds.Contains(t.Receiverid.Value))
            )
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                PayerId = t.Payerid,
                ReceiverId = t.Receiverid,
                Amount = t.Amount,
                Date = t.CreatedAt ?? DateTime.Now,
                // Classify if it's explicitly part of a group activity (has a GroupId)
                IsGroupExpense = t.Groupid == groupId
            })
            .OrderBy(t => t.Date)
            .ToList();

        var explanation = await GetDetailedSettleSummaryAsync(
            groupId,
            initialActivityBalances,
            initialDetailedDebtsMap,
            relevantTransactions,
            memberNames.ToDictionary(kvp => kvp.Key.GetValueOrDefault(), kvp => kvp.Value)
        );

        return SuccessResponse(content:explanation);
    }


      private async Task<SettleSummaryExplanationResponseDto> GetDetailedSettleSummaryAsync(
        Guid groupId,
        Dictionary<Guid, decimal> initialActivityBalances, // Net balances from group activities
        Dictionary<(Guid FromUserId, Guid ToUserId), decimal> initialDetailedDebtsMap, // Who owes whom from activities
        List<TransactionDto> relevantTransactions, // All transactions affecting this group (expenses & settle-ups)
        Dictionary<Guid, string> memberNames)
    {
        var groupName = await _groupService.GetOneAsync(x=>x.Id == groupId);
        // currentBalances starts with the net balances from group activities only.
        var currentBalances = new Dictionary<Guid, decimal>(initialActivityBalances);
        var calculationSteps = new List<SettlementCalculationStepDto>();
        var finalSimplifiedSettlements = new List<SimplifiedSettlementDto>();
        var memberAggregationDetails = new List<MemberAggregationDetailDto>();

        // Step 1: Initial Balances from Group Activities - Explanation (No change here)
        var initialExplanationDescription = new StringBuilder();
        initialExplanationDescription.Append("### Initial State: Balances from Group Activities\n\n");
        initialExplanationDescription.Append("This section details the direct financial obligations arising from group expenses and how they aggregate into each member's overall net balance.\n");

        if (initialDetailedDebtsMap.Any())
        {
            initialExplanationDescription.Append("\n**Detailed Debts (Who Owes Whom based on specific activity shares):**\n");
            foreach (var debt in initialDetailedDebtsMap.OrderBy(d => memberNames.GetValueOrDefault(d.Key.FromUserId))
                                                         .ThenBy(d => memberNames.GetValueOrDefault(d.Key.ToUserId)))
            {
                initialExplanationDescription.Append($"- {memberNames.GetValueOrDefault(debt.Key.FromUserId)} owes {memberNames.GetValueOrDefault(debt.Key.ToUserId)} ${debt.Value:F2}\n");
            }

            initialExplanationDescription.Append("\n**Bridging Direct Debts to Net Balances (Internal Aggregation Logic):**\n");
            initialExplanationDescription.Append("The system aggregates all individual 'who owes whom' debts to determine each person's single net financial position within the group.\n");

            var totalOwedByFromDetailedDebts = new Dictionary<Guid, decimal>();
            foreach (var debt in initialDetailedDebtsMap)
            {
                var fromUser = debt.Key.FromUserId;
                if (!totalOwedByFromDetailedDebts.ContainsKey(fromUser)) totalOwedByFromDetailedDebts[fromUser] = 0;
                totalOwedByFromDetailedDebts[fromUser] += debt.Value;
            }

            var totalOwedToFromDetailedDebts = new Dictionary<Guid, decimal>();
            foreach (var debt in initialDetailedDebtsMap)
            {
                var toUser = debt.Key.ToUserId;
                if (!totalOwedToFromDetailedDebts.ContainsKey(toUser)) totalOwedToFromDetailedDebts[toUser] = 0;
                totalOwedToFromDetailedDebts[toUser] += debt.Value;
            }

            foreach (var memberKvp in memberNames)
            {
                Guid memberId = memberKvp.Key;
                string memberName = memberKvp.Value;

                decimal owedTo = totalOwedToFromDetailedDebts.GetValueOrDefault(memberId);
                decimal owedBy = totalOwedByFromDetailedDebts.GetValueOrDefault(memberId);
                decimal calculatedNetBalance = owedTo - owedBy;
                decimal actualServiceNetBalance = initialActivityBalances.GetValueOrDefault(memberId);

                memberAggregationDetails.Add(new MemberAggregationDetailDto
                {
                    MemberId = memberId,
                    MemberName = memberName,
                    TotalOwedToThem = owedTo,
                    TotalTheyOweOthers = owedBy,
                    CalculatedNetBalance = calculatedNetBalance,
                    ActualServiceNetBalance = actualServiceNetBalance,
                    BalancesMatch = Math.Abs(calculatedNetBalance - actualServiceNetBalance) < 0.01m
                });
            }
        }
        else
        {
            initialExplanationDescription.Append("\n*No specific direct debts from group activities were recorded, or all initial debts self-cancelled.*\n");
        }

        initialExplanationDescription.Append("\n**Overall Net Balances after Group Activities (as determined by the system):**\n");
        foreach (var balance in initialActivityBalances.OrderBy(b => memberNames.GetValueOrDefault(b.Key)))
        {
             initialExplanationDescription.Append($"- {memberNames.GetValueOrDefault(balance.Key)}: ${balance.Value:F2} ({(balance.Value > 0 ? "Creditor" : (balance.Value < 0 ? "Debtor" : "Settled"))})\n");
        }

        var initialDetailedDebtsForResponse = new Dictionary<string, decimal>();
        foreach(var debt in initialDetailedDebtsMap)
        {
            initialDetailedDebtsForResponse[$"{memberNames.GetValueOrDefault(debt.Key.FromUserId)} owes {memberNames.GetValueOrDefault(debt.Key.ToUserId)}"] = debt.Value;
        }

        calculationSteps.Add(new SettlementCalculationStepDto
        {
            Description = initialExplanationDescription.ToString(),
            // This step's balance shows the state *after* initial activities only.
            BalancesAfterStep = new Dictionary<Guid, decimal>(currentBalances)
        });

        // Step 2: Apply relevant transactions to adjust balances (UNIFIED APPLICATION)
        if (relevantTransactions.Any())
        {
            var transactionExplanation = new StringBuilder();
            transactionExplanation.Append("### Transaction Adjustments\n\n");
            transactionExplanation.Append("This section accounts for all payments between group members. These payments directly modify the net balances established from group activities.\n\n");

            // Process all relevant transactions in chronological order to correctly update balances.
            // This ensures manual settle-ups and other direct payments are factored in.
            foreach (var transaction in relevantTransactions)
            {
                // Apply balance adjustments for ALL relevant transactions
                // Payer's balance increases (they paid out, so they are owed more or owe less)
                if (transaction.PayerId.HasValue && currentBalances.ContainsKey(transaction.PayerId.Value))
                {
                    currentBalances[transaction.PayerId.Value] += transaction.Amount;
                }
                // Receiver's balance decreases (they received money, so they are owed less or owe more)
                if (transaction.ReceiverId.HasValue && currentBalances.ContainsKey(transaction.ReceiverId.Value))
                {
                    currentBalances[transaction.ReceiverId.Value] -= transaction.Amount;
                }

                var payerName = memberNames.GetValueOrDefault(transaction.PayerId.GetValueOrDefault(), transaction.PayerId?.ToString() ?? "Unknown Payer");
                var receiverName = memberNames.GetValueOrDefault(transaction.ReceiverId.GetValueOrDefault(), transaction.ReceiverId?.ToString() ?? "Unknown Receiver");

                // Generate explanation based on transaction type for clarity
                if (transaction.IsGroupExpense)
                {
                    transactionExplanation.Append($"- **Group Expense Payment:** {payerName} paid {receiverName} ${transaction.Amount:F2} for group activity. *(Date: {transaction.Date:yyyy-MM-dd})*\n");
                    transactionExplanation.Append($"  * This payment is directly related to a group activity.\n");
                }
                else // This is a manual settle-up or other direct payment between members
                {
                    transactionExplanation.Append($"- **Manual Settle-Up Payment:** {payerName} paid {receiverName} ${transaction.Amount:F2}. *(Date: {transaction.Date:yyyy-MM-dd})*\n");
                    transactionExplanation.Append($"  * This is a direct payment made between members to adjust balances.\n");
                }

                if (!string.IsNullOrWhiteSpace(transaction.Description))
                {
                    transactionExplanation.Append($"  * Description: {transaction.Description}\n");
                }
                transactionExplanation.Append("\n"); // Add a newline after each transaction for better spacing
            }

            // Add this step with the truly adjusted balances after all relevant transactions.
            calculationSteps.Add(new SettlementCalculationStepDto
            {
                Description = transactionExplanation.ToString(),
                BalancesAfterStep = new Dictionary<Guid, decimal>(currentBalances) // This now reflects ALL transactions
            });
        }
        else
        {
             calculationSteps.Add(new SettlementCalculationStepDto
            {
                Description = "### Transaction Adjustments: No manual transactions were recorded.",
                BalancesAfterStep = new Dictionary<Guid, decimal>(currentBalances)
            });
        }

        // --- Core Minimal Settlement Algorithm with Explanation (unchanged logic, input is updated currentBalances) ---
        // activeBalancesForSettlement now correctly starts with balances after ALL initial activities AND manual transactions.
        var activeBalancesForSettlement = currentBalances
            .Where(kvp => Math.Abs(kvp.Value) > 0.01m)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        calculationSteps.Add(new SettlementCalculationStepDto
        {
            Description = "### Minimal Settlement Calculation: Balances After All Adjustments\n\n" +
                          "These are the balances after considering all initial group activities and any manual transactions. The goal is now to find the *fewest* number of payments to clear these outstanding balances.",
            BalancesAfterStep = new Dictionary<Guid, decimal>(activeBalancesForSettlement)
        });

        int settlementStepNumber = 1;
        while (activeBalancesForSettlement.Any(kvp => Math.Abs(kvp.Value) > 0.01m))
        {
            var creditors = activeBalancesForSettlement.Where(b => b.Value > 0).OrderByDescending(b => b.Value).ToList();
            var debtors = activeBalancesForSettlement.Where(b => b.Value < 0).OrderBy(b => b.Value).ToList();

            if (!creditors.Any() || !debtors.Any()) break;

            var creditor = creditors.First();
            var debtor = debtors.First();

            var amountToSettle = Math.Min(creditor.Value, Math.Abs(debtor.Value));

            var settlement = new SimplifiedSettlementDto
            {
                PayerId = debtor.Key,
                PayerName = memberNames.GetValueOrDefault(debtor.Key, debtor.Key.ToString()),
                ReceiverId = creditor.Key,
                ReceiverName = memberNames.GetValueOrDefault(creditor.Key, creditor.Key.ToString()),
                Amount = amountToSettle
            };
            finalSimplifiedSettlements.Add(settlement);

            activeBalancesForSettlement[creditor.Key] -= amountToSettle;
            activeBalancesForSettlement[debtor.Key] += amountToSettle;

            var currentCreditorsExplanation = creditors.Select(c => $"{memberNames.GetValueOrDefault(c.Key, c.Key.ToString())}: +${c.Value:F2}").ToList();
            var currentDebtorsExplanation = debtors.Select(d => $"{memberNames.GetValueOrDefault(d.Key, d.Key.ToString())}: ${d.Value:F2}").ToList();

            calculationSteps.Add(new SettlementCalculationStepDto
            {
                Description = $"### Settlement Step {settlementStepNumber}: {settlement.PayerName} pays {settlement.ReceiverName} ${settlement.Amount:F2}\n\n" +
                              $"*Current Creditors (owed money):* {string.Join(", ", currentCreditorsExplanation)}\n" +
                              $"*Current Debtors (owe money):* {string.Join(", ", currentDebtorsExplanation)}\n\n" +
                              $"To minimize the total number of payments, the algorithm identifies the **largest creditor** ('{settlement.ReceiverName}', owed **${creditor.Value:F2}**) and the **largest debtor** ('{settlement.PayerName}', owes **${Math.Abs(debtor.Value):F2}**). " +
                              $"A direct payment of **${amountToSettle:F2}** is made from '{settlement.PayerName}' to '{settlement.ReceiverName}'. " +
                              $"This amount is the lesser of the creditor's positive balance or the debtor's absolute negative balance, ensuring that at least one of them will have their balance fully settled by this transaction.",
                BalancesAfterStep = new Dictionary<Guid, decimal>(activeBalancesForSettlement),
                SettlementDetail = settlement,
                CreditorsInThisStep = currentCreditorsExplanation,
                DebtorsInThisStep = currentDebtorsExplanation
            });

            settlementStepNumber++;
        }

        var finalZeroedBalances = memberNames.Keys.ToDictionary(id => id, id => 0m);

        calculationSteps.Add(new SettlementCalculationStepDto
        {
            Description = "### Final Balances After All Settlements\n\n" +
                          "All outstanding balances have been settled with the minimum number of transactions.",
            BalancesAfterStep = finalZeroedBalances
        });

        return new SettleSummaryExplanationResponseDto
        {
            GroupId = groupId,
            GroupName = groupName.Groupname,
            MemberNames = memberNames,
            InitialNetBalancesFromActivities = initialActivityBalances,
            InitialDetailedDebtsFromActivities = initialDetailedDebtsForResponse,
            RelevantTransactionsConsidered = relevantTransactions,
            // BalancesAfterAllAdjustments now accurately reflects the state *after* initial activities AND all relevant transactions.
            BalancesAfterAllAdjustments = new Dictionary<Guid, decimal>(currentBalances),
            CalculationSteps = calculationSteps,
            MemberAggregationDetails = memberAggregationDetails,
            FinalSimplifiedSettlements = finalSimplifiedSettlements,
            FinalTransactionCount = finalSimplifiedSettlements.Count
        };
    }

    // GetDetailedGroupDebtsAsync (unchanged)
    private async Task<Dictionary<(Guid FromUserId, Guid ToUserId), decimal>> GetDetailedGroupDebtsAsync(Guid groupId)
    {
        var activities = await _activityService.GetListAsync(
            x => x.Groupid == groupId && !x.Isdeleted,
            query => query
        );

        var debtMap = new Dictionary<(Guid FromUserId, Guid ToUserId), decimal>();

        foreach (var activity in activities)
        {
            if (activity.Paidbyid == null)
                continue;

            var payerId = activity.Paidbyid.Value;

            foreach (var split in activity.ActivitySplits)
            {
                if (split.Userid == null)
                    continue;

                var userId = split.Userid.Value;
                var amount = split.Splitamount;

                if (userId == payerId)
                    continue;

                var key = (FromUserId: userId, ToUserId: payerId);

                if (!debtMap.ContainsKey(key))
                    debtMap[key] = 0;

                debtMap[key] += amount;
            }
        }
        return debtMap;
    }
}