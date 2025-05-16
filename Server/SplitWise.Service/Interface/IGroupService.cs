
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.DTO.Response;

namespace SplitWise.Service.Interface;

public interface IGroupService : IBaseService<Group>
{
    Task<string> CreateGroupAsync(GroupRequest groupRequest);
    Task<List<OnlyGroupResponse>> GetGroupsAsync();
    Task<string> UpdateGroupAsync(GroupUpdateRequest request);
}
