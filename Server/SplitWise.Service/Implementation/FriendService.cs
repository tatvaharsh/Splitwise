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

    // public async Task<FriendResponse> GetAllListQuery()
    // {
    //     Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

    //     // Step 1: Get accepted friends
    //     var friendCollections = await GetListAsync(
    //         x => x.Status == "accepted" && (x.Userid == userId || x.Friendid == userId)
    //     );

    //     var friendIds = friendCollections
    //         .Select(f => f.Userid == userId ? f.Friendid : f.Userid)
    //         .Distinct()
    //         .ToList();

    //     // Step 2: Calculate balances for each friend
    //     var friendBalances = new Dictionary<Guid, decimal>();

    //     foreach (var friendId in friendIds)
    //     {
    //         decimal totalOweLent = 0;

    //         // 1️⃣ Group-wise minimal settlements
    //         var userGroups = await _groupMemberService.GetListAsync(x => x.Memberid == userId && !x.Isdeleted);
    //         var friendGroups = await _groupMemberService.GetListAsync(x => x.Memberid == friendId && !x.Isdeleted);

    //         var sharedGroupIds = userGroups.Select(x => x.Groupid)
    //                                        .Intersect(friendGroups.Select(x => x.Groupid))
    //                                        .ToList();

    //         foreach (var groupId in sharedGroupIds)
    //         {
    //             var netBalances = await _activityService.CalculateNetBalancesForGroupAsync(groupId.Value);

    //             // Apply transactions
    //             var completedTransactions = await _transactionService.GetListAsync(t => t.Groupid == groupId && !t.Isdeleted);
    //             foreach (var txn in completedTransactions)
    //             {
    //                 if (txn.Payerid.HasValue && netBalances.ContainsKey(txn.Payerid.Value))
    //                     netBalances[txn.Payerid.Value] += txn.Amount;

    //                 if (txn.Receiverid.HasValue && netBalances.ContainsKey(txn.Receiverid.Value))
    //                     netBalances[txn.Receiverid.Value] -= txn.Amount;
    //             }

    //             var settlements = _activityService.CalculateMinimalSettlements(netBalances);
    //             foreach (var s in settlements)
    //             {
    //                 if ((s.PayerId == userId && s.ReceiverId == friendId) ||
    //                     (s.PayerId == friendId && s.ReceiverId == userId))
    //                 {
    //                     if (s.PayerId == userId)
    //                         totalOweLent -= s.Amount; // you owe
    //                     else
    //                         totalOweLent += s.Amount; // friend owes
    //                 }
    //             }
    //         }

    //         // 2️⃣ Direct transactions (1:1)
    //         var directExpenses = await _activityService.GetListAsync(
    //             x => !x.Isdeleted && x.Groupid == null &&
    //             (
    //                 (x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == friendId)) ||
    //                 (x.Paidbyid == friendId && x.ActivitySplits.Any(s => s.Userid == userId))
    //             ),
    //             query => query.Include(a => a.ActivitySplits).Include(a => a.Paidby)
    //         );

    //         foreach (var exp in directExpenses)
    //         {
    //             decimal oweLent = 0;
    //             if (exp.Paidbyid == userId)
    //             {
    //                 oweLent = exp.ActivitySplits.Where(s => s.Userid == friendId).Sum(s => s.Splitamount);
    //             }
    //             else if (exp.Paidbyid == friendId)
    //             {
    //                 oweLent = -exp.ActivitySplits.Where(s => s.Userid == userId).Sum(s => s.Splitamount);
    //             }

    //             totalOweLent += oweLent;
    //         }

    //         var oneToOneTxns = await _transactionService.GetListAsync(
    //             x => x.Groupid == null && (
    //                 (x.Payerid == userId && x.Receiverid == friendId) ||
    //                 (x.Payerid == friendId && x.Receiverid == userId)
    //             )
    //         );

    //         foreach (var txn in oneToOneTxns)
    //         {
    //             if (txn.Payerid == userId)
    //             {
    //                 totalOweLent += txn.Amount; // you paid
    //             }
    //             else if (txn.Payerid == friendId)
    //             {
    //                 totalOweLent -= txn.Amount; // friend paid you
    //             }
    //         }

    //         friendBalances[friendId.Value] = totalOweLent;
    //     }

    //     // Step 3: Build response
    //     var acceptedFriends = new List<AcceptedFriendResponse>();

    //     foreach (var friendId in friendIds)
    //     {
    //         var friend = await _userService.GetOneAsync(x => x.Id == friendId);
    //         var latestActivity = await GetLatestActivityBetween(userId, friendId.Value);

    //         acceptedFriends.Add(new AcceptedFriendResponse
    //         {
    //             Id = friend.Id,
    //             Name = friend.Username,
    //             OweLentAmount = friendBalances.ContainsKey(friendId.Value) ? friendBalances[friendId.Value] : 0,
    //             LastActivityDescription = latestActivity
    //         });
    //     }

    //     // 2. Get Pending Friends (kept as it was concise and useful)
    //     var pendingEntities = await GetListAsync(
    //         x => x.Status == "pending" && (x.Userid == userId || x.Friendid == userId),
    //         query => query.Include(x => x.User).Include(x => x.Friend)
    //     );

    //     var pendingFriends = pendingEntities
    //         .Select(p =>
    //         {
    //             return new PendingFriendResponse
    //             {
    //                 FromId = p.Userid.Value,
    //                 FromName = p.User.Username,
    //                 ToId = p.Friendid.Value,
    //                 ToName = p.Friend.Username
    //             };
    //         })
    //         .Where(x => x != null)
    //         .ToList();

    //     return new FriendResponse
    //     {
    //         AcceptedFriends = acceptedFriends,
    //         PendingFriends = pendingFriends
    //     };
    // }

   public async Task<FriendResponse> GetAllListQuery()
{
    Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

    // Step 1: Get accepted friends and their IDs efficiently
    var friendCollections = await GetListAsync(
        x => x.Status == "accepted" && (x.Userid == userId || x.Friendid == userId)
    );

    var friendIds = friendCollections
        .Select(f => f.Userid == userId ? f.Friendid : f.Userid)
        .Distinct()
        .Where(id => id.HasValue)
        .Select(id => id.Value)
        .ToList();

    if (!friendIds.Any())
    {
        return new FriendResponse
        {
            AcceptedFriends = new List<AcceptedFriendResponse>(),
            PendingFriends = await GetPendingFriendsAsync(userId)
        };
    }

    // Pre-fetch all necessary user data for current user and friends in one go
    var allUserIdsForAcceptedFriends = new List<Guid>(friendIds) { userId };
    var friendsData = await _userService.GetListAsync(x => allUserIdsForAcceptedFriends.Contains(x.Id));
    var friendIdToUserMap = friendsData.ToDictionary(u => u.Id);

    // Pre-fetch all group memberships for the current user and all friends
    var groupMemberships = await _groupMemberService.GetListAsync(
        x => allUserIdsForAcceptedFriends.Contains(x.Memberid.Value) && !x.Isdeleted
    );
    var userGroupMap = groupMemberships
        .GroupBy(gm => gm.Memberid.Value)
        .ToDictionary(g => g.Key, g => g.Select(gm => gm.Groupid.Value).ToHashSet());

    // Calculate all shared group IDs once
    var allSharedGroupIds = new HashSet<Guid>();
    if (userGroupMap.TryGetValue(userId, out var currentUserGroups))
    {
        foreach (var friendId in friendIds)
        {
            if (userGroupMap.TryGetValue(friendId, out var currentFriendGroups))
            {
                foreach (var groupId in currentUserGroups.Intersect(currentFriendGroups))
                {
                    allSharedGroupIds.Add(groupId);
                }
            }
        }
    }

    // Fetch all group activities and their splits in one go
    var allGroupActivities = await _activityService.GetListAsync(
        x => allSharedGroupIds.Contains(x.Groupid.Value) && !x.Isdeleted,
        query => query.Include(x => x.ActivitySplits)
    );
    var groupActivitiesByGroup = allGroupActivities.GroupBy(a => a.Groupid.Value).ToDictionary(g => g.Key, g => g.ToList());

    // Fetch all group transactions
    var allGroupTransactions = await _transactionService.GetListAsync(
        x => allSharedGroupIds.Contains(x.Groupid.Value) && !x.Isdeleted
    );
    var groupTransactionsByGroup = allGroupTransactions.GroupBy(t => t.Groupid.Value).ToDictionary(g => g.Key, g => g.ToList());

    // Fetch all direct activities and their splits involving the current user or any friend
    var allDirectActivities = await _activityService.GetListAsync(
        x => x.Groupid == null && !x.Isdeleted &&
             (allUserIdsForAcceptedFriends.Contains(x.Paidbyid.Value) || x.ActivitySplits.Any(s => allUserIdsForAcceptedFriends.Contains(s.Userid.Value))),
        query => query.Include(x => x.ActivitySplits)
    );

    // Fetch all one-to-one transactions involving the current user or any friend
    var allOneToOneTxns = await _transactionService.GetListAsync(
        x => x.Groupid == null && (
             (allUserIdsForAcceptedFriends.Contains(x.Payerid.Value) && allUserIdsForAcceptedFriends.Contains(x.Receiverid.Value))
        )
    );

    // Step 2: Calculate balances for each friend (in-memory processing)
    var friendBalances = new Dictionary<Guid, decimal>();
    var latestActivities = new Dictionary<Guid, Activity>();

    foreach (var friendId in friendIds)
    {
        decimal totalOweLent = 0;
        Activity currentFriendLatestActivity = null; // Use nullable Activity

        // 1️⃣ Group-wise minimal settlements
        if (userGroupMap.TryGetValue(userId, out var currentUserGroupsForFriend) &&
            userGroupMap.TryGetValue(friendId, out var currentFriendGroupsForFriend))
        {
            var sharedGroupIdsForFriend = currentUserGroupsForFriend.Intersect(currentFriendGroupsForFriend).ToHashSet();

            foreach (var groupId in sharedGroupIdsForFriend)
            {
                var netBalances = CalculateNetBalancesForGroup(groupActivitiesByGroup.GetValueOrDefault(groupId, new List<Activity>()));

                // Apply transactions
                if (groupTransactionsByGroup.TryGetValue(groupId, out var groupTransactions))
                {
                    foreach (var txn in groupTransactions)
                    {
                        if (txn.Payerid.HasValue && netBalances.ContainsKey(txn.Payerid.Value))
                            netBalances[txn.Payerid.Value] += txn.Amount;

                        if (txn.Receiverid.HasValue && netBalances.ContainsKey(txn.Receiverid.Value))
                            netBalances[txn.Receiverid.Value] -= txn.Amount;
                    }
                }

                // Pass the user map to avoid N+1 in settlement calculation
                var settlements = CalculateMinimalSettlements(netBalances, friendIdToUserMap);

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

                // Track latest activity for this group
                var latestGroupActivity = groupActivitiesByGroup.GetValueOrDefault(groupId, new List<Activity>())
                                            .OrderByDescending(a => a.CreatedAt)
                                            .FirstOrDefault();
                if (latestGroupActivity != null && (currentFriendLatestActivity == null || latestGroupActivity.CreatedAt > currentFriendLatestActivity.CreatedAt))
                {
                    currentFriendLatestActivity = latestGroupActivity;
                }
            }
        }

        // 2️⃣ Direct transactions (1:1) and activities
        var friendDirectActivities = allDirectActivities
            .Where(exp =>
                (exp.Paidbyid == userId && exp.ActivitySplits.Any(s => s.Userid == friendId && !s.Isdeleted)) ||
                (exp.Paidbyid == friendId && exp.ActivitySplits.Any(s => s.Userid == userId && !s.Isdeleted)) &&
                // Ensure it's truly 1:1 between current user and this specific friend
                exp.ActivitySplits.Where(s => !s.Isdeleted).Select(s => s.Userid).Distinct().Count() == 2 &&
                exp.ActivitySplits.Any(s => s.Userid == userId && !s.Isdeleted) &&
                exp.ActivitySplits.Any(s => s.Userid == friendId && !s.Isdeleted)
            )
            .ToList();

        foreach (var exp in friendDirectActivities)
        {
            decimal oweLent = 0;
            if (exp.Paidbyid == userId)
            {
                oweLent = exp.ActivitySplits.Where(s => s.Userid == friendId && !s.Isdeleted).Sum(s => s.Splitamount);
            }
            else if (exp.Paidbyid == friendId)
            {
                oweLent = -exp.ActivitySplits.Where(s => s.Userid == userId && !s.Isdeleted).Sum(s => s.Splitamount);
            }
            totalOweLent += oweLent;

            if (exp.CreatedAt > (currentFriendLatestActivity?.CreatedAt ?? DateTime.MinValue))
            {
                currentFriendLatestActivity = exp;
            }
        }

        var friendOneToOneTxns = allOneToOneTxns
            .Where(txn =>
                (txn.Payerid == userId && txn.Receiverid == friendId) ||
                (txn.Payerid == friendId && txn.Receiverid == userId)
            )
            .ToList();

        foreach (var txn in friendOneToOneTxns)
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

        friendBalances[friendId] = totalOweLent;
        latestActivities[friendId] = currentFriendLatestActivity; // Store the latest activity object
    }

    // Step 3: Build response
    var acceptedFriends = new List<AcceptedFriendResponse>();

    foreach (var friendId in friendIds)
    {
        var friend = friendIdToUserMap.GetValueOrDefault(friendId);
        if (friend == null) continue;

        acceptedFriends.Add(new AcceptedFriendResponse
        {
            Id = friend.Id,
            Name = friend.Username,
            OweLentAmount = friendBalances.GetValueOrDefault(friendId, 0),
            LastActivityDescription = latestActivities.GetValueOrDefault(friendId)?.Description ?? string.Empty
        });
    }

    return new FriendResponse
    {
        AcceptedFriends = acceptedFriends,
        PendingFriends = await GetPendingFriendsAsync(userId)
    };
}

// Helper method to consolidate pending friends logic
private async Task<List<PendingFriendResponse>> GetPendingFriendsAsync(Guid userId)
{
    var pendingEntities = await GetListAsync(
        x => x.Status == "pending" && (x.Userid == userId || x.Friendid == userId),
        query => query.Include(x => x.User).Include(x => x.Friend)
    );

    return pendingEntities
        .Select(p => new PendingFriendResponse
        {
            FromId = p.Userid.Value,
            FromName = p.User.Username,
            ToId = p.Friendid.Value,
            ToName = p.Friend.Username
        })
        .ToList();
}

// Modified to accept pre-fetched activities
public Dictionary<Guid, decimal> CalculateNetBalancesForGroup(List<Activity> activities)
{
    var netBalances = new Dictionary<Guid, decimal>();

    foreach (var activity in activities)
    {
        if (activity.Paidbyid == null)
            continue;

        var payerId = activity.Paidbyid.Value;
        var totalAmount = activity.Amount.GetValueOrDefault();

        netBalances.TryAdd(payerId, 0);
        netBalances[payerId] += totalAmount;

        foreach (var split in activity.ActivitySplits)
        {
            if (split.Userid == null)
                continue;

            var userId = split.Userid.Value;
            var shareAmount = split.Splitamount;

            netBalances.TryAdd(userId, 0);
            netBalances[userId] -= shareAmount;
        }
    }

    return netBalances;
}

// Optimized to accept pre-fetched user names, making it truly in-memory
public List<SettleSummaryDto> CalculateMinimalSettlements(Dictionary<Guid, decimal> netBalances, Dictionary<Guid, User> userMap)
{
    Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();

    var settlements = new List<SettleSummaryDto>();

    var creditors = new Queue<KeyValuePair<Guid, decimal>>(
        netBalances.Where(x => x.Value > 0)
                    .OrderByDescending(x => x.Value));

    var debtors = new Queue<KeyValuePair<Guid, decimal>>(
        netBalances.Where(x => x.Value < 0)
                    .OrderBy(x => x.Value));

    while (creditors.Any() && debtors.Any())
    {
        var creditor = creditors.Dequeue();
        var debtor = debtors.Dequeue();

        var amountToSettle = Math.Min(creditor.Value, Math.Abs(debtor.Value));

        settlements.Add(new SettleSummaryDto
        {
            PayerId = debtor.Key,
            PayerName = debtor.Key == userId ? "You" : userMap.GetValueOrDefault(debtor.Key)?.Username ?? "Unknown",
            ReceiverId = creditor.Key,
            ReceiverName = creditor.Key == userId ? "You" : userMap.GetValueOrDefault(creditor.Key)?.Username ?? "Unknown",
            Amount = amountToSettle
        });

        var remainingCreditor = creditor.Value - amountToSettle;
        var remainingDebtor = debtor.Value + amountToSettle;

        if (remainingCreditor > 0)
            creditors.Enqueue(new KeyValuePair<Guid, decimal>(creditor.Key, remainingCreditor));

        if (remainingDebtor < 0)
            debtors.Enqueue(new KeyValuePair<Guid, decimal>(debtor.Key, remainingDebtor));
    }

    return settlements
    .Where(s => s.PayerId == userId || s.ReceiverId == userId)
    .ToList();
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

    // 1. Fetch 1:1 expenses
    var directExpenses = await _activityService.GetListAsync(
        x => !x.Isdeleted && x.Groupid == null &&
             ((x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == id)) ||
              (x.Paidbyid == id && x.ActivitySplits.Any(s => s.Userid == userId))),
        query => query.Include(a => a.ActivitySplits).ThenInclude(s => s.User).Include(a => a.Paidby)
    ) ?? new List<Activity>();

    // 2. Fetch group memberships and common groups
    var groupMembers = await _groupMemberService.GetListAsync(
        x => (x.Memberid == userId || x.Memberid == id) && x.Groupid != null,
        query => query.Include(x => x.Group)
    );

    var commonGroupIds = groupMembers
        .GroupBy(x => x.Groupid)
        .Where(g => g.Count() > 1)
        .Select(g => g.Key)
        .ToList();

    // 3. Fetch group expenses
    var groupExpenses = await _activityService.GetListAsync(
        x => !x.Isdeleted && x.Groupid != null && commonGroupIds.Contains(x.Groupid.Value) &&
             ((x.Paidbyid == userId && x.ActivitySplits.Any(s => s.Userid == id)) ||
              (x.Paidbyid == id && x.ActivitySplits.Any(s => s.Userid == userId))),
        query => query.Include(a => a.Group).Include(a => a.ActivitySplits).ThenInclude(s => s.User).Include(a => a.Paidby)
    ) ?? new List<Activity>();

    // 4. Fetch direct (1:1) settle-up transactions
    var directTransactions = await _transactionService.GetListAsync(
        x => x.Groupid == null && !x.Isdeleted &&
             ((x.Payerid == userId && x.Receiverid == id) || (x.Payerid == id && x.Receiverid == userId)),
        query => query.Include(x => x.Payer).Include(x => x.Receiver)
    ) ?? new List<Transaction>();

    // 5. Fetch group settle-up transactions
    var groupTransactions = await _transactionService.GetListAsync(
        x => x.Groupid != null && commonGroupIds.Contains(x.Groupid.Value) && !x.Isdeleted &&
             ((x.Payerid == userId && x.Receiverid == id) || (x.Payerid == id && x.Receiverid == userId)),
        query => query.Include(x => x.Payer).Include(x => x.Receiver)
    ) ?? new List<Transaction>();

    // List to hold all combined expense and transaction items
    List<GroupItemResponse> allItems = new List<GroupItemResponse>();

    // 6. Process expense entries (direct and group) and map to GetExpenseByGroupId
    var expenseResponses = directExpenses.Concat(groupExpenses).Select(exp =>
    {
        decimal oweLent = 0;
        if (exp.Paidbyid == userId)
        {
            // Current user paid, they lent the amount owed by the friend
            oweLent = exp.ActivitySplits.FirstOrDefault(s => s.Userid == id)?.Splitamount ?? 0;
        }
        else if (exp.Paidbyid == id)
        {
            // Friend paid, current user owes their share
            oweLent = -(exp.ActivitySplits.FirstOrDefault(s => s.Userid == userId)?.Splitamount ?? 0);
        }

        return new GroupItemResponse
        {
            Id = exp.Id,
            Type = "Expense",
            Description = exp.Group != null
                ? (string.IsNullOrEmpty(exp.Description) ? exp.Group.Groupname : $"{exp.Group.Groupname} - {exp.Description}")
                : exp.Description ?? "",
            PayerName = exp.Paidbyid == userId ? "You" : exp.Paidby?.Username ?? friendUser.Username,
            ReceiverName = null, // Not applicable for expenses
            Amount = exp.Amount ?? 0,
            Date = exp.Time ?? DateTime.UtcNow,
            OweLentAmount = oweLent, // Impact of this specific expense
            OweLentAmountOverall = 0 ,
            OrderDate = exp.CreatedAt ?? DateTime.UtcNow
        };
    }).ToList();

    allItems.AddRange(expenseResponses);

    // 7. Process settle-up transactions (direct and group) and map to GetExpenseByGroupId
    var transactionResponses = directTransactions.Concat(groupTransactions).Select(txn =>
    {
        decimal oweLent = 0;
        string payerName = txn.Payerid == userId ? "You" : txn.Payer?.Username ?? friendUser.Username;
        string receiverName = txn.Receiverid == userId ? "you" : txn.Receiver?.Username ?? friendUser.Username;

        if (txn.Payerid == userId)
        {
            // Current user paid, they lent the amount
            oweLent = txn.Amount;
        }
        else if (txn.Receiverid == userId)
        {
            // Current user received, they owe the amount (negative impact)
            oweLent = -txn.Amount;
        }

        return new GroupItemResponse
        {
            Id = txn.Id,
            Type = "SettleUp",
            Description = $"{payerName} settled {receiverName}",
            PayerName = payerName,
            ReceiverName = receiverName,
            Amount = txn.Amount,
            Date = txn.Time ?? DateTime.UtcNow,
            OweLentAmount = oweLent, // Impact of this specific transaction
            OweLentAmountOverall = 0,
            OrderDate = txn.CreatedAt ?? DateTime.UtcNow
        };
    }).ToList();

    allItems.AddRange(transactionResponses);

    // 8. Sort the combined list by Date in descending order (most recent first, e.g., s4, e3, s1, e2, e1)
    allItems = allItems.OrderByDescending(item => item.OrderDate).ToList();

    // 9. Calculate the overall owe/lent balance
    decimal totalOweLentOverall = allItems.Sum(item => item.OweLentAmount);

    // 10. Update OweLentAmountOverall for all items
    foreach (var item in allItems)
    {
        item.OweLentAmountOverall = totalOweLentOverall;
    }

    // 11. Return response
    return new GetFriendresponse
    {
        Id = friendUser.Id,
        Name = friendUser.Username,
        Expenses = allItems,
        OweLentAmountOverall = totalOweLentOverall // +ve: friend owes you, -ve: you owe friend
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