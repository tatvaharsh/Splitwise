using SplitWise.Domain;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.Generic.Helper;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class ActivityService(IBaseRepository<Activity> baseRepository) : BaseService<Activity>(baseRepository), IActivityService
{
    
}
