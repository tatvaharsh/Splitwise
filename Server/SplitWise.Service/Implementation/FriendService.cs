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
        // 1. Fetch all relevant group activities
        List<Activity> groupEntities = await _activityService.GetListAsync(
            x => x.Groupid == groupId && x.Paidbyid != null,
            query => query.Include(x => x.ActivitySplits)
        );

        // 2. Fetch all transactions in the group related to this member
        List<Transaction> memberTransactions = await _transactionService.GetListAsync(
            x => x.Groupid == groupId && (x.Receiverid == memberId || x.Payerid == memberId)
        );

        decimal netBalance = 0;

        // 3. Calculate total from activities
        foreach (var activity in groupEntities)
        {
            if (activity.Paidbyid == memberId)
            {
                // Member paid the total, so others owe him
                decimal totalOwedByOthers = activity.ActivitySplits.Sum(s => s.Splitamount) 
                    - activity.ActivitySplits.FirstOrDefault(s => s.Userid == memberId)?.Splitamount ?? 0;
                netBalance += totalOwedByOthers;
            }
            else
            {
                // Member owes to someone else
                decimal amountOwed = activity.ActivitySplits.FirstOrDefault(s => s.Userid == memberId)?.Splitamount ?? 0;
                netBalance -= amountOwed;
            }
        }

        // 4. Adjust balance based on transactions (settlements)
        foreach (var tx in memberTransactions)
        {
            if (tx.Payerid == memberId)
            {
                // Member paid someone -> decrease what he owes
                netBalance += tx.Amount;
            }
            else if (tx.Receiverid == memberId)
            {
                // Member received money -> decrease what others owe him
                netBalance -= tx.Amount;
            }
        }

        // 5. If net balance is not zero, then there's still outstanding
        return netBalance != 0;
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

        // Step 2: Calculate balances for each friend
        var friendBalances = new Dictionary<Guid, decimal>();

        foreach (var friendId in friendIds)
        {
            decimal totalOweLent = 0;

            // 1️⃣ Group-wise minimal settlements
            var userGroups = await _groupMemberService.GetListAsync(x => x.Memberid == userId && !x.Isdeleted);
            var friendGroups = await _groupMemberService.GetListAsync(x => x.Memberid == friendId && !x.Isdeleted);

            var sharedGroupIds = userGroups.Select(x => x.Groupid)
                                           .Intersect(friendGroups.Select(x => x.Groupid))
                                           .ToList();

            foreach (var groupId in sharedGroupIds)
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
                    if ((s.PayerId == userId && s.ReceiverId == friendId) ||
                        (s.PayerId == friendId && s.ReceiverId == userId))
                    {
                        if (s.PayerId == userId)
                            totalOweLent -= s.Amount; // you owe
                        else
                            totalOweLent += s.Amount; // friend owes
                    }
                }
            }

            // 2️⃣ Direct transactions (1:1)
            var directExpenses = await _activityService.GetListAsync(
                x => !x.Isdeleted && x.Groupid == null &&
                (
                    (x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == friendId)) ||
                    (x.Paidbyid == friendId && x.ActivitySplits.Any(s => s.Userid == userId))
                ),
                query => query.Include(a => a.ActivitySplits).Include(a => a.Paidby)
            );

            foreach (var exp in directExpenses)
            {
                decimal oweLent = 0;
                if (exp.Paidbyid == userId)
                {
                    oweLent = exp.ActivitySplits.Where(s => s.Userid == friendId).Sum(s => s.Splitamount);
                }
                else if (exp.Paidbyid == friendId)
                {
                    oweLent = -exp.ActivitySplits.Where(s => s.Userid == userId).Sum(s => s.Splitamount);
                }

                totalOweLent += oweLent;
            }

            var oneToOneTxns = await _transactionService.GetListAsync(
                x => x.Groupid == null && (
                    (x.Payerid == userId && x.Receiverid == friendId) ||
                    (x.Payerid == friendId && x.Receiverid == userId)
                )
            );

            foreach (var txn in oneToOneTxns)
            {
                if (txn.Payerid == userId)
                {
                    totalOweLent += txn.Amount; // you paid
                }
                else if (txn.Payerid == friendId)
                {
                    totalOweLent -= txn.Amount; // friend paid you
                }
            }

            friendBalances[friendId.Value] = totalOweLent;
        }

        // Step 3: Build response
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
                return new PendingFriendResponse
                {
                    FromId = p.Userid.Value,
                    FromName = p.User.Username,
                    ToId = p.Friendid.Value,
                    ToName = p.Friend.Username
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