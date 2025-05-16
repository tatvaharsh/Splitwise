using Microsoft.EntityFrameworkCore;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Response;
using SplitWise.Repository.Interface;

namespace SplitWise.Service.Implementation;

public class FriendService(IBaseRepository<FriendCollection> baseRepository) : BaseService<FriendCollection>(baseRepository), IFriendService
{
    public async Task<List<MemberResponse>> GetFriendsAsync()
    {
        // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);
        List<FriendCollection> friendlist = await GetListAsync(
            x => x.Status == "accepted" && (x.Userid == userId || x.Friendid == userId),
            query => query
                .Include(x => x.User)
                .Include(x => x.Friend)
        );
        
        List<MemberResponse> friendResponses = friendlist.Select(x =>
        {
            User friend = x.Userid == userId ? x.Friend : x.User;
            return new MemberResponse
            {
                Id = friend.Id,
                Name = friend.Username,
            };
        }).ToList();

        return friendResponses;
    }

}
