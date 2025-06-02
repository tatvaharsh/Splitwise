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

public class FriendService(IBaseRepository<FriendCollection> baseRepository, IActivityService activityService, IGroupService groupService, IUserService userService, IEmailService emailService,
 IGroupMemberService groupMemberService, ITransactionService transactionService, IExpenseService expenseService, IGroupMemberRepository groupMemberRepository, IActivityLoggerService activityLoggerService,
 IAppContextService appContextService) : BaseService<FriendCollection>(baseRepository), IFriendService
{
    private readonly IGroupMemberService _groupMemberService = groupMemberService;
    private readonly IAppContextService _appContextService = appContextService;
    private readonly IExpenseService _expenseService = expenseService;
    private readonly ITransactionService _transactionService = transactionService;
    private readonly IEmailService _emailService = emailService;
    private readonly IActivityLoggerService _activityLoggerService = activityLoggerService;
    private readonly IUserService _userService = userService;
    private readonly IGroupMemberRepository _groupMemberRepository = groupMemberRepository;
    private readonly IGroupService _groupService = groupService;
    private readonly IActivityService _activityService = activityService;

    public async Task<string> AcceptFriendAsync(Guid friendId)
    {
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
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
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

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
            var path = Path.Combine(Directory.GetCurrentDirectory(), @"..\", "SplitWise.Domain", "Generic", "Templates", "InviteFriend.html");

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

        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
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
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        User user = await _userService.GetOneAsync(x => x.Id == userId) ?? throw new Exception("User not found");

        GroupMember groupMember = await _groupMemberService.GetOneAsync(x => x.Memberid == id && x.Groupid == groupid, query => query.Include(x => x.Member)
        .Include(x => x.Group)) ?? throw new Exception();
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

        return SplitWiseConstants.RECORD_DELETED;
    }

    public async Task<FriendResponse> GetAllListQuery()
    {
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

        // Step 1: Get accepted friends
        var friendCollections = await GetListAsync(
            x => x.Status == "accepted" && (x.Userid == userId || x.Friendid == userId)
        );

        var friendIds = friendCollections
            .Select(f => f.Userid == userId ? f.Friendid : f.Userid)
            .Distinct()
            .ToList();

        // Step 2: Get user's group memberships
        var userGroups = await _groupMemberService.GetListAsync(x => x.Memberid == userId && !x.Isdeleted);
        var userGroupIds = userGroups.Select(g => g.Groupid).ToList();

        // Step 3: Get all members of those groups
        var allGroupMembers = await _groupMemberService.GetListAsync(x =>
            userGroupIds.Contains(x.Groupid) && !x.Isdeleted
        );

        // Step 4: Group to members map
        var groupToMembers = allGroupMembers
            .GroupBy(g => g.Groupid)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Memberid).ToList());

        // Step 5: Global balances map
        Dictionary<Guid, decimal> globalBalances = new();

        foreach (var groupId in userGroupIds)
        {
            var groupMembers = groupToMembers[groupId];

            if (!groupMembers.Any(id => friendIds.Contains(id)))
                continue;

            var groupBalances = await _activityService.CalculateNetBalancesForGroupAsync(groupId.Value);

            var transactions = await _transactionService.GetListAsync(
                t => t.Groupid == groupId && !t.Isdeleted
            );

            foreach (var t in transactions)
            {
                if (t.Payerid.HasValue)
                    groupBalances[t.Payerid.Value] += t.Amount;

                if (t.Receiverid.HasValue)
                    groupBalances[t.Receiverid.Value] -= t.Amount;
            }

            foreach (var kv in groupBalances)
            {
                if (!globalBalances.ContainsKey(kv.Key))
                    globalBalances[kv.Key] = 0;

                globalBalances[kv.Key] += kv.Value;
            }
        }

        // Step 6: Add 1-on-1 activities (non-group) using ActivitySplit
        var directActivities = await _activityService.GetListAsync(a =>
            !a.Groupid.HasValue && !a.Isdeleted &&
            (a.Paidbyid == userId || friendIds.Contains(a.Paidbyid.Value))
        );

        foreach (var activity in directActivities)
        {
            var payerId = activity.Paidbyid.Value;
            var totalAmount = activity.Amount;

            var splits = await _expenseService.GetListAsync(x => x.Activityid == activity.Id);

            if (!globalBalances.ContainsKey(payerId))
                globalBalances[payerId] = 0;
            globalBalances[payerId] += (totalAmount ?? 0m);

            foreach (var split in splits)
            {
                if (split.Userid.HasValue)
                {
                    if (!globalBalances.ContainsKey(split.Userid.Value))
                        globalBalances[split.Userid.Value] = 0;
                    globalBalances[split.Userid.Value] -= split.Splitamount;
                }
            }
        }

        // 🔄 NEW: Step 6.1 — Add 1-on-1 direct settlements (non-group transactions)
        var directTransactions = await _transactionService.GetListAsync(
            t => t.Groupid == null && !t.Isdeleted &&
                 (t.Payerid == userId || t.Receiverid == userId ||
                  friendIds.Contains(t.Payerid.Value) || friendIds.Contains(t.Receiverid.Value))
        );

        foreach (var t in directTransactions)
        {
            if (t.Payerid.HasValue)
            {
                if (!globalBalances.ContainsKey(t.Payerid.Value))
                    globalBalances[t.Payerid.Value] = 0;
                globalBalances[t.Payerid.Value] += t.Amount;
            }

            if (t.Receiverid.HasValue)
            {
                if (!globalBalances.ContainsKey(t.Receiverid.Value))
                    globalBalances[t.Receiverid.Value] = 0;
                globalBalances[t.Receiverid.Value] -= t.Amount;
            }
        }

        // Step 7: Simplify balances
        var settlements = _activityService.CalculateMinimalSettlements(globalBalances);

        // Step 8: Filter only between user and their friends
        var friendBalances = new Dictionary<Guid, decimal>();

        foreach (var s in settlements)
        {
            if (s.PayerId == userId && friendIds.Contains(s.ReceiverId))
            {
                friendBalances[s.ReceiverId] = -s.Amount;
            }
            else if (s.ReceiverId == userId && friendIds.Contains(s.PayerId))
            {
                friendBalances[s.PayerId] = s.Amount;
            }
        }

        // Step 9: Build response
        var acceptedFriends = new List<AcceptedFriendResponse>();

        foreach (var friendId in friendIds)
        {
            var friend = await _userService.GetOneAsync(x => x.Id == friendId);
            var latestActivity = await GetLatestActivityBetween(userId, friendId.Value);

            acceptedFriends.Add(new AcceptedFriendResponse
            {
                Id = friend.Id,
                Name = friend.Username,
                OweLentAmount = friendBalances.ContainsKey(friendId.Value) ? friendBalances[friendId.Value] : 0,
                LastActivityDescription = latestActivity
            });
        }

        // 2. Get Pending Friends (kept as it was concise and useful)
        var pendingEntities = await GetListAsync(
            x => x.Status == "pending" && (x.Userid == userId || x.Friendid == userId),
            query => query.Include(x => x.User).Include(x => x.Friend)
        );

        var pendingFriends = pendingEntities
            .Select(p =>
            {
                var friendId = p.Userid == userId ? p.Friendid : p.Userid;
                var friendName = p.Userid == userId ? p.Friend?.Username : p.User?.Username;

                if (friendId == null || friendName == null)
                    return null;

                return new PendingFriendResponse
                {
                    Id = friendId.Value,
                    Name = friendName
                };
            })
            .Where(x => x != null)
            .ToList();

        return new FriendResponse
        {
            AcceptedFriends = acceptedFriends,
            PendingFriends = pendingFriends
        };
    }

    private async Task<string> GetLatestActivityBetween(Guid userId, Guid friendId)
    {
        // --- 1. Shared Group Activities ---
        var userGroups = await _groupMemberService.GetListAsync(x => x.Memberid == userId && !x.Isdeleted);
        var friendGroups = await _groupMemberService.GetListAsync(x => x.Memberid == friendId && !x.Isdeleted);

        var sharedGroupIds = userGroups.Select(x => x.Groupid)
                                       .Intersect(friendGroups.Select(x => x.Groupid))
                                       .ToList();

        Activity latestGroupActivity = null;

        if (sharedGroupIds.Any())
        {
            var groupActivities = await _activityService.GetListAsync(
                x => sharedGroupIds.Contains(x.Groupid.Value) && !x.Isdeleted &&
                     (x.Paidbyid == userId || x.Paidbyid == friendId),
                query => query.Include(x => x.ActivitySplits)
            );

            latestGroupActivity = groupActivities
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();
        }

        // --- 2. Direct Activities (1-on-1) ---
        var directActivities = await _activityService.GetListAsync(
            x => x.Groupid == null && !x.Isdeleted &&
                 (x.Paidbyid == userId || x.Paidbyid == friendId),
            query => query.Include(x => x.ActivitySplits)
        );

        var latestDirectActivity = directActivities
            .Where(a =>
                a.ActivitySplits.Where(s => !s.Isdeleted).Select(s => s.Userid).Distinct().Count() == 2 &&
                a.ActivitySplits.Any(s => s.Userid == userId && !s.Isdeleted) &&
                a.ActivitySplits.Any(s => s.Userid == friendId && !s.Isdeleted)
            )
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        // --- 3. Pick the most recent one ---
        Activity latest = latestGroupActivity;

        if (latestDirectActivity != null &&
            (latest == null || latestDirectActivity.CreatedAt > latest.CreatedAt))
        {
            latest = latestDirectActivity;
        }

        return latest?.Description ?? string.Empty;
    }

    public async Task<GetFriendresponse> GetFriendDetails(Guid id)
    {
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

        // Get friendship relation
        var friendRelation = await GetOneAsync(
            x => (x.Userid == userId && x.Friendid == id) || (x.Userid == id && x.Friendid == userId),
            query => query.Include(x => x.User).Include(x => x.Friend)
        ) ?? throw new Exception("Friend not found");

        var friendUser = friendRelation.Userid == userId ? friendRelation.Friend : friendRelation.User;

        // 1:1 expenses
        var directExpenses = await _activityService.GetListAsync(
            x => !x.Isdeleted && x.Groupid == null &&
            (
                (x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == id)) ||
                (x.Paidbyid == id && x.ActivitySplits.Any(s => s.Userid == userId))
            ),
            query => query.Include(a => a.ActivitySplits).Include(a => a.Paidby)
        );

        // Group memberships
        var groupMembers = await _groupMemberService.GetListAsync(
            x => (x.Memberid == userId || x.Memberid == id) && x.Groupid != null,
            query => query.Include(x => x.Group)
        );

        var commonGroupIds = groupMembers
            .GroupBy(x => x.Groupid)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        // Group expenses
        var groupExpenses = await _activityService.GetListAsync(
            x => !x.Isdeleted && x.Groupid != null && commonGroupIds.Contains(x.Groupid.Value) &&
            (
                (x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == id)) ||
                (x.Paidbyid == id && x.ActivitySplits.Any(s => s.Userid == userId))
            ),
            query => query.Include(a => a.Group).Include(a => a.ActivitySplits).Include(a => a.Paidby)
        );

        var allExpenses = directExpenses.Concat(groupExpenses).OrderByDescending(a => a.CreatedAt).ToList();

        var expenseList = allExpenses.Select(exp =>
        {
            decimal oweLent = 0;
            if (exp.Paidbyid == userId)
            {
                oweLent = exp.ActivitySplits.Where(s => s.Userid == id).Sum(s => s.Splitamount);
            }
            else if (exp.Paidbyid == id)
            {
                oweLent = -exp.ActivitySplits.Where(s => s.Userid == userId).Sum(s => s.Splitamount);
            }

            return new GetExpenseByGroupId
            {
                Id = exp.Id,
                Description = exp.Group != null
                    ? (string.IsNullOrEmpty(exp.Description) ? exp.Group.Groupname : $"{exp.Group.Groupname} - {exp.Description}")
                    : exp.Description ?? "",
                PayerName = exp.Paidbyid == userId ? "You" : exp.Paidby?.Username ?? "",
                Amount = exp.Amount,
                Date = exp.Time,
                OweLentAmount = Math.Abs(oweLent),
                OweLentAmountOverall = oweLent
            };
        }).ToList();

        decimal totalOweLent = 0;

        // 1️⃣ Group-wise minimal settlements
        foreach (var groupId in commonGroupIds)
        {
            var netBalances = await _activityService.CalculateNetBalancesForGroupAsync(groupId.Value);

            // Apply transactions
            var completedTransactions = await _transactionService.GetListAsync(t => t.Groupid == groupId && !t.Isdeleted);
            foreach (var txn in completedTransactions)
            {
                if (txn.Payerid.HasValue && netBalances.ContainsKey(txn.Payerid.Value))
                    netBalances[txn.Payerid.Value] += txn.Amount;

                if (txn.Receiverid.HasValue && netBalances.ContainsKey(txn.Receiverid.Value))
                    netBalances[txn.Receiverid.Value] -= txn.Amount;
            }

            var settlements = _activityService.CalculateMinimalSettlements(netBalances);
            foreach (var s in settlements)
            {
                if ((s.PayerId == userId && s.ReceiverId == id) ||
                    (s.PayerId == id && s.ReceiverId == userId))
                {
                    if (s.PayerId == userId)
                        totalOweLent -= s.Amount; // you owe
                    else
                        totalOweLent += s.Amount; // friend owes
                }
            }
        }

        // 2️⃣ Direct transactions (1:1)
        foreach (var exp in directExpenses)
        {
            decimal oweLent = 0;
            if (exp.Paidbyid == userId)
            {
                oweLent = exp.ActivitySplits.Where(s => s.Userid == id).Sum(s => s.Splitamount);
            }
            else if (exp.Paidbyid == id)
            {
                oweLent = -exp.ActivitySplits.Where(s => s.Userid == userId).Sum(s => s.Splitamount);
            }

            totalOweLent += oweLent;
        }

        var oneToOneTxns = await _transactionService.GetListAsync(
            x => x.Groupid == null && (
                (x.Payerid == userId && x.Receiverid == id) ||
                (x.Payerid == id && x.Receiverid == userId)
            )
        );

        foreach (var txn in oneToOneTxns)
        {
            if (txn.Payerid == userId)
            {
                totalOweLent += txn.Amount; // you paid
            }
            else if (txn.Payerid == id)
            {
                totalOweLent -= txn.Amount; // friend paid you
            }
        }

        return new GetFriendresponse
        {
            Id = friendUser.Id,
            Name = friendUser.Username,
            Expenses = expenseList,
            OweLentAmountOverall = totalOweLent // +ve: friend owes you, -ve: you owe friend
        };
    }

    public async Task<List<MemberResponse>> GetFriendsAsync()
    {
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
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
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
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
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

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