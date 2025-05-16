using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Response;
using SplitWise.Service.Interface;

namespace SplitWise.Service;

public interface IFriendService : IBaseService<FriendCollection>
{
    Task<List<MemberResponse>> GetFriendsAsync();

}
