using SplitWise.Domain.Data;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class ActivityLoggerService(IBaseRepository<ActivityLog> baseRepository) : BaseService<ActivityLog>(baseRepository), IActivityLoggerService
{
    public async Task LogAsync(Guid userId, string description)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        await AddAsync(log);
    }

}
