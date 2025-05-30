
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.DTO.Response;

namespace SplitWise.Service.Interface;

public interface IActivityService : IBaseService<Activity>
{
    Task<string> CreateActivityAsync(CreateActivityRequest request);
    Task<string> EditActivityAsync(UpdateActivityRequest command);
    Task<UpdateActivityRequest> GetExpenseByIdAsync(Guid id);
    Task<List<ActivityResponse>> GetAllListQuery();
    Task<Dictionary<Guid, decimal>> CalculateNetBalancesForGroupAsync(Guid groupId);
    List<SettleSummaryDto> CalculateMinimalSettlements(Dictionary<Guid, decimal> activityBalances);
    Task<string> SettleUpAsync(SettleUpRequest request);
    Task<Dictionary<Guid, decimal>> CalculateNetBalancesForFriendsAsync(Guid friend1Id, Guid friend2Id);

}