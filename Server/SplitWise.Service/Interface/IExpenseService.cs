using SplitWise.Domain.Data;

namespace SplitWise.Service.Interface;

public interface IExpenseService : IBaseService<ActivitySplit>
{
    Task DeleteSplitsByActivityIdAsync(Guid activityId);
}
