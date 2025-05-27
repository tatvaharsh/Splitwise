using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SplitWise.Domain;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.DTO.Response;
using SplitWise.Domain.Generic.Helper;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class FriendService(IBaseRepository<FriendCollection> baseRepository, IActivityService activityService,IGroupService groupService, IUserService userService, IEmailService emailService,
 IGroupMemberService groupMemberService, IGroupMemberRepository groupMemberRepository, IActivityLoggerService activityLoggerService) : BaseService<FriendCollection>(baseRepository), IFriendService
{
    private readonly IGroupMemberService _groupMemberService = groupMemberService;
    private readonly IEmailService _emailService = emailService;
    private readonly IActivityLoggerService _activityLoggerService = activityLoggerService;
    private readonly IUserService _userService = userService;
    private readonly IGroupMemberRepository _groupMemberRepository = groupMemberRepository;
    private readonly IGroupService _groupService = groupService;
    private readonly IActivityService _activityService = activityService;

    public async Task<string> AcceptFriendAsync(Guid friendId)
    {
        // Simulate logged-in user (replace with _appContextService.GetUserId() in real app)
        var userId = Guid.Parse("78c89439-8cb5-4e93-8565-de9b7cf6c6ae");

        // Find the friend request
        var friendRequest = GetOneAsync(
            x => (x.Userid == userId && x.Friendid == friendId) || (x.Userid == friendId && x.Friendid == userId),
            query => query.Include(x => x.User).Include(x => x.Friend)
        ).Result;

        if (friendRequest == null || friendRequest.Status != "pending")
        {
            throw new Exception();
        }

        // Update status to accepted
        friendRequest.Status = "accepted";
        await UpdateAsync(friendRequest);

        return SplitWiseConstants.RECORD_UPDATED;
    }


    public async Task<string> AddFriendAsync(AddFriend friend)
    {
        // Simulate logged-in user (replace with _appContextService.GetUserId() in real app)
        var userId = Guid.Parse("78c89439-8cb5-4e93-8565-de9b7cf6c6ae");

        // Check if email is already registered
        var existingUser = await _userService.GetOneAsync(x => x.Email == friend.Email);
        var loggedInUser = await _userService.GetOneAsync(x => x.Id == userId);
        if (existingUser != null)
        {
            // Already a user, add to friend collection
            bool alreadyFriend = await GetOneAsync(
                x => (x.Userid == userId && x.Friendid == existingUser.Id) || (x.Userid == existingUser.Id && x.Friendid == userId),
                query => query.Include(x => x.User).Include(x => x.Friend)
            ) != null;
            if (alreadyFriend)
            {
                return "You are already connected or have a pending request.";
            }

            var friendEntity = new FriendCollection
            {
                Userid = userId,
                Friendid = existingUser.Id,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            await AddAsync(friendEntity);
            return SplitWiseConstants.RECORD_CREATED;
        }
        else
        {
            string subject = string.Format(SplitWiseConstants.INVITATION);
            var path = Path.Combine(Directory.GetCurrentDirectory(), @"..\","SplitWise.Domain", "Generic", "Templates", "InviteFriend.html");

            string htmlBody = await FileHelper.ReadFileFromPath(path);
            htmlBody = htmlBody.Replace("{{FriendName}}", loggedInUser.Username);

            await _emailService.SendEmailAsync([friend.Email], subject, htmlBody);
        }
        return SplitWiseConstants.RECORD_CREATED;
    }


    public async Task<string> AddFriendIntoGroup(Guid friendId, Guid groupId)
    {
        string groupName = string.Empty;

        var group = await _groupService.GetByIdAsync(groupId);
        if (group != null)
        {
            groupName = group.Groupname;
        }
        
        // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);
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

        await _activityLoggerService.LogAsync(userId, $"You added '{result.Friend.Username}' to the group '{groupName}'");
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
        // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);

        User user = await _userService.GetOneAsync(x => x.Id == userId) ?? throw new Exception("User not found");

        GroupMember groupMember = await _groupMemberService.GetOneAsync(x => x.Memberid == id && x.Groupid == groupid, query => query.Include(x => x.Member)
        .Include(x=>x.Group)) ?? throw new Exception();
        await _groupMemberRepository.DeleteMember(id, groupid);

        await _activityLoggerService.LogAsync(userId,
            $"You removed '{groupMember.Member.Username}' from the group '{groupMember.Group.Groupname}'");

        await _activityLoggerService.LogAsync(id,
            $"{user.Username} removed you from the group '{groupMember.Group.Groupname}'");

        var remainingMembers = await _groupMemberService.GetListAsync(
            gm => gm.Groupid == groupid
        );

        foreach (var member in remainingMembers)
        {
            if (member.Memberid.HasValue && member.Memberid.Value != userId && member.Memberid.Value != id)
            {
                await _activityLoggerService.LogAsync(member.Memberid.Value,
                    $"'{groupMember.Member.Username}' was removed from the group '{groupMember.Group.Groupname}'.");
            }
        }

        return Domain.SplitWiseConstants.RECORD_DELETED;
    }

    public async Task<FriendResponse> GetAllListQuery()
    {
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);

        // 1. Get Accepted Friends
        var friendEntities = await GetListAsync(
            x => x.Status == "accepted" && (x.Userid == userId || x.Friendid == userId),
            query => query.Include(x => x.User).Include(x => x.Friend)
        );

        var acceptedFriends = new List<AcceptedFriendResponse>();

        foreach (var friend in friendEntities)
        {
            var friendId = friend.Userid == userId ? friend.Friendid : friend.Userid;
            var friendName = friend.Userid == userId ? friend.Friend.Username : friend.User.Username;

            // Get all shared activities
            var activities = await _activityService.GetListAsync(
                x =>
                    !x.Isdeleted &&
                    (
                        (x.Groupid != null &&
                        x.ActivitySplits.Any(s => s.Userid == userId) &&
                        x.ActivitySplits.Any(s => s.Userid == friendId)) ||

                        (x.Groupid == null &&
                        (
                            (x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == friendId)) ||
                            (x.Paidbyid == friendId && x.ActivitySplits.Any(s => s.Userid == userId))
                        ))
                    ) &&
                    (
                        (x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == friendId)) ||
                        (x.Paidbyid == friendId && x.ActivitySplits.Any(s => s.Userid == userId)) ||
                        (x.Paidbyid == friendId && friendId == userId) ||
                        (x.Paidbyid == userId && friendId == userId)
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

            acceptedFriends.Add(new AcceptedFriendResponse
            {
                Id = friendId ?? Guid.Empty,
                Name = friendName,
                LastActivityDescription = lastActivity?.Description,
                OweLentAmount = totalOweLent
            });
        }

        // 2. Get Pending Friends
        var pendingEntities = await GetListAsync(
            x => x.Status == "pending" && (x.Userid == userId || x.Friendid == userId),
            query => query.Include(x => x.User).Include(x => x.Friend)
        );

        var pendingFriends = new List<PendingFriendResponse>();

        foreach (var pending in pendingEntities)
        {
            var friendId = pending.Userid == userId ? pending.Friendid : pending.Userid;
            var friendName = pending.Userid == userId ? pending.Friend?.Username : pending.User?.Username;

            if (friendId != null && friendName != null)
            {
                pendingFriends.Add(new PendingFriendResponse
                {
                    Id = friendId.Value,
                    Name = friendName
                });
            }
        }

        // 3. Return Combined Response
        return new FriendResponse
        {
            AcceptedFriends = acceptedFriends,
            PendingFriends = pendingFriends
        };
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

    public async Task<string> RejectFriendAsync(Guid friendId)
    {
        // Simulate logged-in user (replace with _appContextService.GetUserId() in real app)
        var userId = Guid.Parse("78c89439-8cb5-4e93-8565-de9b7cf6c6ae");

        // Find the friend request
        var friendRequest = GetOneAsync(
            x => (x.Userid == userId && x.Friendid == friendId) || (x.Userid == friendId && x.Friendid == userId),
            query => query.Include(x => x.User).Include(x => x.Friend)
        ).Result;

        if (friendRequest == null || friendRequest.Status != "pending")
        {
            throw new Exception();
        }

        // Update status to accepted
        friendRequest.Status = "rejected";
        await UpdateAsync(friendRequest);

        return SplitWiseConstants.RECORD_UPDATED;
    }

}
