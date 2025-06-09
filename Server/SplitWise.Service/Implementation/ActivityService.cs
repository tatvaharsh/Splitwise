using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using SplitWise.Domain;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.DTO.Response;
using SplitWise.Domain.Generic.Helper;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class ActivityService(IBaseRepository<Activity> baseRepository, IExpenseService expenseService, IGroupService groupService, IUserService userService, ITransactionService transactionService,
IActivityLoggerService activityLoggerService, IAppContextService appContextService) : BaseService<Activity>(baseRepository), IActivityService
{
    private readonly IExpenseService _expenseService = expenseService;
    private readonly IAppContextService _appContextService = appContextService;
    private readonly ITransactionService _transactionService = transactionService;
    private readonly IUserService _userService = userService;
    private readonly IGroupService _groupService = groupService;
    private readonly IActivityLoggerService _activityLoggerService = activityLoggerService;
    public async Task<string> CreateActivityAsync(CreateActivityRequest request)
    {
        string groupName = string.Empty;

        if (request.GroupId != null)
        {
            var group = await _groupService.GetByIdAsync(request.GroupId.Value);
            if (group != null)
            {
                groupName = group.Groupname;
            }
        }
        Activity activity = new()
        {
            Description = request.Description,
            Paidbyid = request.PaidById,
            Groupid = request.GroupId,
            Amount = request.Amount,
            Time = request.Date,
        };
        await AddAsync(activity);

        List<ActivitySplit> splits = request.Splits.Select(split => new ActivitySplit
        {
            Activityid = activity.Id,
            Userid = split.UserId,
            Splitamount = split.SplitAmount
        }).ToList();

        await _expenseService.AddRangeAsync(splits);

        await _activityLoggerService.LogAsync(request.PaidById, $"You paid ₹{request.Amount} for {request.Description}" +
            (request.GroupId != null ? $" in {groupName}" : ""));

        // Log for each participant (excluding the payer)
        foreach (var split in request.Splits)
        {
            if (split.UserId != request.PaidById)
            {
                await _activityLoggerService.LogAsync(
                    split.UserId,
                    $"You owe ₹{split.SplitAmount} for '{request.Description}'" +
                    (request.GroupId != null ? $" in group {groupName}" : "")
                );
            }
        }
        return SplitWiseConstants.RECORD_CREATED;
    }

    public async Task<string> EditActivityAsync(UpdateActivityRequest request)
    {
        string groupName = string.Empty;

        if (request.GroupId != null)
        {
            var group = await _groupService.GetByIdAsync(request.GroupId.Value);
            if (group != null)
            {
                groupName = group.Groupname;
            }
        }
        Activity activity = await GetByIdAsync(request.Id) ?? throw new NotFoundException();

        activity.Description = request.Description;
        activity.Paidbyid = request.PaidById;
        activity.Groupid = request.GroupId;
        activity.Amount = request.Amount;
        activity.Time = request.Date;
        activity.UpdatedAt = request.Date;

        await UpdateAsync(activity);
        await _expenseService.DeleteSplitsByActivityIdAsync(request.Id);

        List<ActivitySplit> splits = request.Splits.Select(split => new ActivitySplit
        {
            Activityid = activity.Id,
            Userid = split.UserId,
            Splitamount = split.SplitAmount
        }).ToList();

        await _expenseService.AddRangeAsync(splits);

        await _activityLoggerService.LogAsync(request.PaidById, $"You paid ₹{request.Amount} for {request.Description}" +
        (request.GroupId != null ? $" in {groupName}" : ""));

        // Log for each participant (excluding the payer)
        foreach (var split in request.Splits)
        {
            if (split.UserId != request.PaidById)
            {
                await _activityLoggerService.LogAsync(
                    split.UserId,
                    $"You owe ₹{split.SplitAmount} for '{request.Description}'" +
                    (request.GroupId != null ? $" in group {groupName}" : "")
                );
            }
        }
        return SplitWiseConstants.RECORD_UPDATED;
    }

    public async Task<List<ActivityResponse>> GetAllListQuery()
    {
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        // Fetch activities where user is either payer or involved in splits
        var activities = await GetListAsync(a => a.Paidbyid == userId || a.ActivitySplits.Any(s => s.Userid == userId),
            query => query.Include(a => a.ActivitySplits)
                        .ThenInclude(s => s.User)
            .Include(a => a.Group));

        var activityResponses = activities.OrderByDescending(x => x.CreatedAt).Select(exp => new ActivityResponse
        {
            Id = exp.Id,
            Date = exp.Time,
            Amount = exp.Amount,
            Description = exp.Paidbyid == userId
                ? $"You paid ₹{exp.Amount} for {exp.Description}" +
                    (exp.Group != null ? $" in {exp.Group.Groupname}" : "")

                : $"{exp.Paidby?.Username ?? "Someone"} paid ₹{exp.Amount} for {exp.Description}" +
                    (exp.Group != null ? $" in {exp.Group.Groupname}" : "")
        }).ToList();


        return activityResponses;
    }

    public async Task<string> DeleteActivityAsync(Guid id)
    {
        Activity activity = await GetByIdAsync(id) ?? throw new NotFoundException();
        activity.Isdeleted = true;
        await UpdateAsync(activity);

        var splits = await GetListAsync(x => x.Id == id, query => query.Include(x => x.ActivitySplits)); // Assume this method exists

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
        foreach (var split in splits)
        {
            if (split.Paidbyid != activity.Paidbyid)
            {
                string userMessage = activity.Groupid != null
                    ? $"The expense '{activity.Description}' was deleted in group '{groupName}'"
                    : $"The expense '{activity.Description}' was deleted";

                if (split.Paidbyid.HasValue)
                {
                    await _activityLoggerService.LogAsync(split.Paidbyid.Value, userMessage);
                }
            }
        }
        return SplitWiseConstants.RECORD_DELETED;
    }

    public async Task<ActivityResponse> GetActivityByIdAsync(Guid id)
    {
        Activity activity = await GetByIdAsync(id) ?? throw new NotFoundException();
        return new ActivityResponse
        {
            Id = activity.Id,
            Date = activity.Time,
            Amount = activity.Amount,
            Description = activity.Description ?? ""
        };
    }

    public async Task<UpdateActivityRequest> GetExpenseByIdAsync(Guid id)
    {
        var activity = await GetByIdAsync(id) ?? throw new Exception("Activity not found");

        var splits = await GetOneAsync(x => x.Id == id, query => query
            .Include(x => x.ActivitySplits)
            .ThenInclude(x => x.User)) ?? throw new Exception("Activity not found");

        return new UpdateActivityRequest
        {
            Id = activity.Id,
            Description = activity.Description,
            PaidById = activity.Paidbyid ?? new Guid(),
            GroupId = activity.Groupid,
            Amount = activity.Amount ?? 0,
            Date = activity.Time ?? DateTime.UtcNow,
            Splits = splits.ActivitySplits.Select(s => new ActivitySplitRequest
            {
                UserId = s.Activity.ActivitySplits.FirstOrDefault(x => x.Userid == s.Userid)?.Userid ?? new Guid(),
                SplitAmount = s.Activity.ActivitySplits.FirstOrDefault(x => x.Userid == s.Userid)?.Splitamount ?? 0
            }).ToList()
        };
    }

    public async Task<Dictionary<Guid, decimal>> CalculateNetBalancesForGroupAsync(Guid groupId)
    {
        var activities = await GetListAsync(
            x => x.Groupid == groupId && !x.Isdeleted,
            query => query.Include(x => x.ActivitySplits)
                        .ThenInclude(x => x.User)
        );

        var netBalances = new Dictionary<Guid, decimal>();

        foreach (var activity in activities)
        {
            if (activity.Paidbyid == null)
                continue;

            var payerId = activity.Paidbyid.Value;
            var totalAmount = activity.Amount.GetValueOrDefault(); // Handle nullable decimal

            if (!netBalances.ContainsKey(payerId))
                netBalances[payerId] = 0;

            netBalances[payerId] += totalAmount;

            foreach (var split in activity.ActivitySplits)
            {
                if (split.Userid == null)
                    continue;

                var userId = split.Userid.Value;
                var shareAmount = split.Splitamount; // Handle non-nullable decimal

                if (!netBalances.ContainsKey(userId))
                    netBalances[userId] = 0;

                netBalances[userId] -= shareAmount;
            }
        }

        return netBalances;
    }

    public List<SettleSummaryDto> CalculateMinimalSettlements(Dictionary<Guid, decimal> netBalances)
    {
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

        var settlements = new List<SettleSummaryDto>();

        var creditors = new Queue<KeyValuePair<Guid, decimal>>(
            netBalances.Where(x => x.Value > 0)
                    .OrderByDescending(x => x.Value));

        var debtors = new Queue<KeyValuePair<Guid, decimal>>(
            netBalances.Where(x => x.Value < 0)
                    .OrderBy(x => x.Value));

        while (creditors.Any() && debtors.Any())
        {
            var creditor = creditors.Dequeue();
            var debtor = debtors.Dequeue();

            var amountToSettle = Math.Min(creditor.Value, Math.Abs(debtor.Value));
            var payerUser = _userService.GetOneAsync(x => x.Id == debtor.Key).Result;
            var receiverUser = _userService.GetOneAsync(x => x.Id == creditor.Key).Result;
            settlements.Add(new SettleSummaryDto
            {
                PayerId = debtor.Key,
                PayerName = debtor.Key == userId ? "You" : payerUser.Username,
                ReceiverId = creditor.Key,
                ReceiverName = creditor.Key == userId ? "You" : receiverUser.Username,
                Amount = amountToSettle
            });

            var remainingCreditor = creditor.Value - amountToSettle;
            var remainingDebtor = debtor.Value + amountToSettle;

            if (remainingCreditor > 0)
                creditors.Enqueue(new KeyValuePair<Guid, decimal>(creditor.Key, remainingCreditor));

            if (remainingDebtor < 0)
                debtors.Enqueue(new KeyValuePair<Guid, decimal>(debtor.Key, remainingDebtor));
        }

        return settlements
        .Where(s => s.PayerId == userId || s.ReceiverId == userId)
        .ToList();
    }

    public async Task<string> SettleUpAsync(SettleUpRequest request)
    {
        var settlement = new Transaction
        {
            Payerid = request.PayerId,
            Receiverid = request.ReceiverId,
            Amount = request.Amount,
            Groupid = request.GroupId,
            Time = DateTime.UtcNow
        };
        await _transactionService.AddAsync(settlement);
        return SplitWiseConstants.RECORD_CREATED;
    }


   public async Task<FriendBalancesSummary> CalculateNetBalancesForFriendsAsync(Guid friend1Id, Guid friend2Id)
    {
        var groupNetBalancesPerGroup = new Dictionary<Guid, Dictionary<Guid, decimal>>();
        var oneToOneNetBalances = new Dictionary<Guid, decimal>
    {
        { friend1Id, 0m },
        { friend2Id, 0m }
    };
 
        var relevantActivities = await GetListAsync(
            x => ((x.Paidbyid == friend1Id || x.Paidbyid == friend2Id) ||
                  x.ActivitySplits.Any(s => s.Userid == friend1Id || s.Userid == friend2Id)) && !x.Isdeleted,
            query => query.Include(x => x.ActivitySplits)
        );
 
        foreach (var activity in relevantActivities)
        {
            var payerId = activity.Paidbyid;
            var totalAmount = activity.Amount.GetValueOrDefault();
            bool isGroupExpense = activity.Groupid.HasValue && activity.Groupid != Guid.Empty;
 
            if (isGroupExpense && activity.Groupid.HasValue)
            {
                Guid currentGroupId = activity.Groupid.Value;
                if (!groupNetBalancesPerGroup.ContainsKey(currentGroupId))
                {
                    groupNetBalancesPerGroup[currentGroupId] = new Dictionary<Guid, decimal>
                {
                    { friend1Id, 0m },
                    { friend2Id, 0m }
                };
                }
 
                // Apply payer amount for group expenses
                if (payerId == friend1Id && groupNetBalancesPerGroup[currentGroupId].ContainsKey(friend1Id))
                {
                    groupNetBalancesPerGroup[currentGroupId][friend1Id] += totalAmount;
                }
                else if (payerId == friend2Id && groupNetBalancesPerGroup[currentGroupId].ContainsKey(friend2Id))
                {
                    groupNetBalancesPerGroup[currentGroupId][friend2Id] += totalAmount;
                }
 
                // Apply split amounts for group expenses
                foreach (var split in activity.ActivitySplits)
                {
                    var userId = split.Userid;
                    var shareAmount = split.Splitamount;
 
                    if (userId.HasValue && (userId.Value == friend1Id || userId.Value == friend2Id) && groupNetBalancesPerGroup[currentGroupId].ContainsKey(userId.Value))
                    {
                        groupNetBalancesPerGroup[currentGroupId][userId.Value] -= shareAmount;
                    }
                }
            }
            else // One-to-one expense
            {
                if((payerId == friend1Id || payerId == friend2Id) &&
                        activity.ActivitySplits.Select(s => s.Userid).Distinct().ToHashSet().IsSupersetOf(new Guid?[] { friend1Id, friend2Id }))
                {
                    if (payerId == friend1Id && oneToOneNetBalances.ContainsKey(friend1Id))
                {
                    oneToOneNetBalances[friend1Id] += totalAmount;
                }
                else if (payerId == friend2Id && oneToOneNetBalances.ContainsKey(friend2Id))
                {
                    oneToOneNetBalances[friend2Id] += totalAmount;
                }
 
                foreach (var split in activity.ActivitySplits)
                {
                    var userId = split.Userid;
                    var shareAmount = split.Splitamount;
 
                    if (userId.HasValue && (userId.Value == friend1Id || userId.Value == friend2Id) && oneToOneNetBalances.ContainsKey(userId.Value))
                    {
                        oneToOneNetBalances[userId.Value] -= shareAmount;
                    }
                }
                }
            }
        }
 
        return new FriendBalancesSummary
        {
            GroupBalancesPerGroup = groupNetBalancesPerGroup,
            OneToOneBalances = oneToOneNetBalances
        };
    }
    public List<SettleSummaryDto> CalculateMinimalSettlement(Dictionary<Guid, decimal> netBalances, Guid? groupId = null)
    {
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var settlements = new List<SettleSummaryDto>();

        var creditors = new Queue<KeyValuePair<Guid, decimal>>(
            netBalances.Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value));

        var debtors = new Queue<KeyValuePair<Guid, decimal>>(
            netBalances.Where(x => x.Value < 0)
                .OrderBy(x => x.Value));

        while (creditors.Any() && debtors.Any())
        {
            var creditor = creditors.Dequeue();
            var debtor = debtors.Dequeue();

            var amountToSettle = Math.Min(creditor.Value, Math.Abs(debtor.Value));
            var payerUser = _userService.GetOneAsync(x => x.Id == debtor.Key).Result;
            var receiverUser = _userService.GetOneAsync(x => x.Id == creditor.Key).Result;

            settlements.Add(new SettleSummaryDto
            {
                PayerId = debtor.Key,
                PayerName = debtor.Key == userId ? "You" : payerUser.Username,
                ReceiverId = creditor.Key,
                ReceiverName = creditor.Key == userId ? "You" : receiverUser.Username,
                Amount = amountToSettle,
                GroupId = groupId // Set the GroupId here
            });

            var remainingCreditor = creditor.Value - amountToSettle;
            var remainingDebtor = debtor.Value + amountToSettle;

            if (remainingCreditor > 0)
                creditors.Enqueue(new KeyValuePair<Guid, decimal>(creditor.Key, remainingCreditor));

            if (remainingDebtor < 0)
                debtors.Enqueue(new KeyValuePair<Guid, decimal>(debtor.Key, remainingDebtor));
        }

        return settlements
        .Where(s => s.PayerId == userId || s.ReceiverId == userId)
        .ToList();
    }
}