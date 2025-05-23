using SplitWise.Domain.Data;
using SplitWise.Repository.Interface;

namespace SplitWise.Service.Interface;

public interface IActivityLoggerService: IBaseService<ActivityLog>
{
    Task LogAsync(Guid userId, string description);
}
