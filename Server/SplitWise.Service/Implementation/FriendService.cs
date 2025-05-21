using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Response;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class FriendService(IBaseRepository<FriendCollection> baseRepository, IActivityService activityService,
 IGroupMemberService groupMemberService, IGroupMemberRepository groupMemberRepository) : BaseService<FriendCollection>(baseRepository), IFriendService
{
    private readonly IGroupMemberService _groupMemberService = groupMemberService;
    private readonly IGroupMemberRepository _groupMemberRepository = groupMemberRepository;
    private readonly IActivityService _activityService = activityService;
    
    public async Task<string> AddFriendIntoGroup(Guid friendId, Guid groupId)
    {
        var result = await GetOneAsync(
            x => x.Friendid == friendId || x.Userid == friendId,
            query => query
                .Include(x => x.User)
                .Include(x => x.Friend)
                .Include(x => x.User.GroupMembers)
                    .ThenInclude(gm => gm.Group)
        );
        GroupMember groupMember = new()
        {
            Groupid = groupId,
            Memberid = friendId,
            JoinedAt = DateTime.UtcNow
        };
        await _groupMemberService.AddAsync(groupMember);
        Activity activityEntity = new()
        {
            Description = $"You added '{result.Friend.Username}' to the group '{result.User.Groups.FirstOrDefault().Groupname}'",
            Groupid = groupId,
            UserInvolvement = false,
            CreatedAt = DateTime.UtcNow,
        };
        await _activityService.AddAsync(activityEntity);
        return Domain.SplitWiseConstants.RECORD_CREATED;
    }

    public async Task<bool> CheckOutstanding(Guid memberId, Guid groupId)
    {
        List<Activity> groupEntities = await _activityService.GetListAsync(
                x => x.Groupid == groupId && x.Paidbyid != null,
                query => query.Include(x => x.ActivitySplits)
            );

            decimal totalOweLent = 0;

            foreach (var groupEntity in groupEntities)
            {
                decimal owelentAmount = 0;

                if (groupEntity.Paidbyid == memberId)
                {
                    // User lent to others
                    owelentAmount = groupEntity.ActivitySplits.Sum(split => split.Splitamount)
                        - groupEntity.ActivitySplits.FirstOrDefault(split => split.Userid == memberId)?.Splitamount ?? 0;
                }
                else
                {
                    // User owes
                    owelentAmount = groupEntity.ActivitySplits.FirstOrDefault(split => split.Userid == memberId)?.Splitamount ?? 0;
                    owelentAmount *= -1;
                }

                totalOweLent += owelentAmount;
            }

            // Return true if outstanding exists (positive or negative), false if settled
            return totalOweLent != 0;

    }


    public async Task<string> DeleteMemberFromGroup(Guid id, Guid groupid)
    {
        GroupMember groupMember = await _groupMemberService.GetOneAsync(x => x.Memberid == id && x.Groupid == groupid, query => query.Include(x => x.Member)
        .Include(x=>x.Group)) ?? throw new Exception();
        await _groupMemberRepository.DeleteMember(id, groupid);
        Activity activityEntity = new()
        {
            Description = $"You removed '{groupMember.Member.Username}' from the group '{groupMember.Group.Groupname}'",
            Groupid = groupMember.Groupid,
            UserInvolvement = false,
            CreatedAt = DateTime.UtcNow,
        };
        await _activityService.AddAsync(activityEntity);
        return Domain.SplitWiseConstants.RECORD_DELETED;
    }


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

    public async Task<List<MemberResponse>> GetFriendsDropdown(Guid groupId)
    {
         // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);

        var groupMembers = await _groupMemberService.GetListAsync(x => true);
        var existingMemberIds = groupMembers
            .Where(gm => gm.Groupid == groupId)
            .Select(gm => gm.Memberid)
            .ToList();

        List<FriendCollection> friendlist = await GetListAsync(
            x => x.Status == "accepted"
                && (x.Userid == userId || x.Friendid == userId)
                && !existingMemberIds.Contains(x.Userid == userId ? x.Friendid : x.Userid),
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
