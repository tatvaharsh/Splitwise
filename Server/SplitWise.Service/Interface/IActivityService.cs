
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;

namespace SplitWise.Service.Interface;

public interface IActivityService : IBaseService<Activity>
{
    Task<string> CreateActivityAsync(CreateActivityRequest request);
    Task<string> EditActivityAsync(UpdateActivityRequest command);
}
