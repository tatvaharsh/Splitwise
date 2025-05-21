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

    public async Task<List<FriendResponse>> GetAllListQuery()
    {
        // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);

        var friendEntities = await GetListAsync(
            x => x.Status == "accepted" && (x.Userid == userId || x.Friendid == userId),
            query => query.Include(x => x.User).Include(x => x.Friend)
        );

        var friendResponses = new List<FriendResponse>();

        foreach (var friend in friendEntities)
        {
            var friendId = friend.Userid == userId ? friend.Friendid : friend.Userid;
            var friendName = friend.Userid == userId ? friend.Friend.Username : friend.User.Username;

            // Shared group check
            var groupMembers = await _groupMemberService.GetListAsync(
                x => x.Memberid == userId || x.Memberid == friendId
            );

            var sharedGroupIds = groupMembers
                .GroupBy(x => x.Groupid)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            // Fetch related activities
            var activities = await _activityService.GetListAsync(
                x =>
                    !x.Isdeleted &&
                    (
                        (x.Groupid != null && sharedGroupIds.Contains(x.Groupid.Value)) ||
                        (x.Groupid == null && (
                            (x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == friendId)) ||
                            (x.Paidbyid == friendId && x.ActivitySplits.Any(s => s.Userid == userId))
                        ))
                    ),
                query => query.Include(a => a.ActivitySplits)
            );

            decimal totalOweLent = 0;
            Activity? lastActivity = null;

            foreach (var activity in activities)
            {
                var userSplit = activity.ActivitySplits.FirstOrDefault(s => s.Userid == userId);
                var friendSplit = activity.ActivitySplits.FirstOrDefault(s => s.Userid == friendId);
                if (userSplit == null || friendSplit == null) continue;

                decimal oweLent = 0;

                if (activity.Paidbyid == userId)
                    oweLent = friendSplit.Splitamount;
                else if (activity.Paidbyid == friendId)
                    oweLent = -userSplit.Splitamount;

                totalOweLent += oweLent;

                if (lastActivity == null || activity.CreatedAt > lastActivity.CreatedAt)
                    lastActivity = activity;
            }

            friendResponses.Add(new FriendResponse
            {
                Id = (friend.Userid == userId ? friend.Friendid : friend.Userid )?? Guid.Empty,
                Name = friendName,
                LastActivityDescription = lastActivity?.Description,
                OweLentAmount = totalOweLent
            });
        }
        return friendResponses;
    }

    public async Task<GetFriendresponse> GetFriendDetails(Guid id)
    {
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);

        // Get friendship relation
        var friendRelation = await GetOneAsync(
            x => (x.Userid == userId && x.Friendid == id) || (x.Userid == id && x.Friendid == userId),
            query => query.Include(x => x.User).Include(x => x.Friend)
        ) ?? throw new Exception("Friend not found");

        // Get actual friend user (not current user)
        var friendUser = friendRelation.Userid == userId ? friendRelation.Friend : friendRelation.User;

        // Get direct 1-to-1 expenses
        var directExpenses = await _activityService.GetListAsync(
            x => !x.Isdeleted && x.Groupid == null &&
            (
                (x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == id)) ||
                (x.Paidbyid == id && x.ActivitySplits.Any(s => s.Userid == userId))
            ),
            query => query
                .Include(a => a.ActivitySplits)
                .Include(a => a.Paidby)
        );

        // Get group expenses (shared groups)
        var groupMembers = await _groupMemberService.GetListAsync(
            x => x.Memberid == userId || x.Memberid == id && x.Groupid != null,
            query => query.Include(x => x.Group)
        );

        var commonGroupIds = groupMembers
            .GroupBy(x => x.Groupid)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        var groupExpenses = await _activityService.GetListAsync(
            x => !x.Isdeleted && x.Groupid != null && commonGroupIds.Contains(x.Groupid.Value) &&
            (
                (x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == id)) ||
                (x.Paidbyid == id && x.ActivitySplits.Any(s => s.Userid == userId))
            ),
            query => query
                .Include(a => a.Group)
                .Include(a => a.ActivitySplits)
                .Include(a => a.Paidby)
        );

        var allExpenses = directExpenses.Concat(groupExpenses).OrderByDescending(a => a.CreatedAt).ToList();

        var expenseList = allExpenses.Select(exp => new GetExpenseByGroupId
        {
            Id = exp.Id,
            Description = exp.Group != null
            ? (string.IsNullOrEmpty(exp.Description) ? exp.Group.Groupname : $"{exp.Group.Groupname} - {exp.Description}")
            : exp.Description ?? "",
            PayerName = exp.Paidbyid == userId ? "You" : exp.Paidby?.Username ?? "",
            Amount = exp.Amount,
            Date = exp.Time,
            OweLentAmount = Math.Abs(
                exp.Paidbyid == userId
                    ? exp.ActivitySplits.Where(s => s.Userid == id).Sum(s => s.Splitamount)       // You paid, they owe
                    : exp.Paidbyid == id
                        ? -exp.ActivitySplits.Where(s => s.Userid == userId).Sum(s => s.Splitamount) // They paid, you owe
                        : 0
            ),
            OweLentAmountOverall =  exp.Paidbyid == userId
            ? exp.ActivitySplits.Where(s => s.Userid == id).Sum(s => s.Splitamount)
            : exp.Paidbyid == id
                ? -exp.ActivitySplits.Where(s => s.Userid == userId).Sum(s => s.Splitamount)
                : 0

        }).ToList();

        var totalOweLent = expenseList.Sum(x => x.OweLentAmountOverall ?? 0);

        return new GetFriendresponse
        {
            Id = friendUser.Id,
            Name = friendUser.Username,
            Expenses = expenseList,
            OweLentAmountOverall = totalOweLent
        };
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
