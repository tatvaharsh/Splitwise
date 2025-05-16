using SplitWise.Domain;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.Generic.Helper;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class ActivityService(IBaseRepository<Activity> baseRepository, IExpenseService expenseService) : BaseService<Activity>(baseRepository), IActivityService
{
    private readonly IExpenseService _expenseService = expenseService;
    public async Task<string> CreateActivityAsync(CreateActivityRequest request)
    {
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
        return SplitWiseConstants.RECORD_CREATED;
    }

    public async Task<string> EditActivityAsync(UpdateActivityRequest request)
    {
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
        return SplitWiseConstants.RECORD_UPDATED;
    }
}
