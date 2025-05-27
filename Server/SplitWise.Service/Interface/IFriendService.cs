using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.DTO.Response;
using SplitWise.Service.Interface;

namespace SplitWise.Service;

public interface IFriendService : IBaseService<FriendCollection>
{
    Task<string> AcceptFriendAsync(Guid friendId);
    Task<string> AddFriendAsync(AddFriend friend);
    Task<string> AddFriendIntoGroup(Guid friendId, Guid groupId);
    Task<bool> CheckOutstanding(Guid memberId, Guid groupId);
    Task<string> DeleteMemberFromGroup(Guid id, Guid groupId);
    Task<FriendResponse> GetAllListQuery();
    Task<GetFriendresponse> GetFriendDetails(Guid id);
    Task<List<MemberResponse>> GetFriendsAsync();
    Task<List<MemberResponse>> GetFriendsDropdown(Guid groupId);
    Task<string> RejectFriendAsync(Guid friendId);

}
