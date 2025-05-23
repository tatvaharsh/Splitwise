using Microsoft.EntityFrameworkCore;
using SplitWise.Domain;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.DTO.Response;
using SplitWise.Domain.Generic.Helper;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class ActivityService(IBaseRepository<Activity> baseRepository, IExpenseService expenseService, IGroupService groupService,
IActivityLoggerService activityLoggerService) : BaseService<Activity>(baseRepository), IActivityService
{
    private readonly IExpenseService _expenseService = expenseService;
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
        // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);

        // Fetch activities where user is either payer or involved in splits
        var activities = await GetListAsync(a => a.Paidbyid == userId || a.ActivitySplits.Any(s => s.Userid == userId),
            query => query.Include(a => a.ActivitySplits)
                        .ThenInclude(s => s.User) 
            .Include(a => a.Group)); 

        var activityResponses = activities.OrderByDescending(x =>x.CreatedAt).Select(exp => new ActivityResponse
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

        var splits = await GetOneAsync(x=>x.Id == id, query => query
            .Include(x => x.ActivitySplits)
            .ThenInclude(x => x.User)) ?? throw new Exception("Activity not found");

        return new UpdateActivityRequest
        {
            Id = activity.Id,
            Description = activity.Description,
            PaidById = activity.Paidbyid ?? new Guid(),
            GroupId = activity.Groupid,
            Amount = activity.Amount ?? 0 ,
            Date = activity.Time ?? DateTime.UtcNow,
            Splits = splits.ActivitySplits.Select(s => new ActivitySplitRequest
            {
                UserId = s.Activity.ActivitySplits.FirstOrDefault(x => x.Userid == s.Userid)?.Userid ?? new Guid(),
                SplitAmount = s.Activity.ActivitySplits.FirstOrDefault(x => x.Userid == s.Userid)?.Splitamount ?? 0 
            }).ToList()
        };
    }
}
