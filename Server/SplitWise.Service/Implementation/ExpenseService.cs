using SplitWise.Domain.Data;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class ExpenseService(IBaseRepository<ActivitySplit> baseRepository) : BaseService<ActivitySplit>(baseRepository), IExpenseService
{
    public async Task DeleteSplitsByActivityIdAsync(Guid activityId)
    {
        List<ActivitySplit> splits = await GetListAsync(x => x.Activityid == activityId) ?? throw new NotFoundException();
        if (splits.Any())
        {
            DeleteRangeAsync(splits.Select(x => x.Id).ToList());
        }
    }

}
