namespace SplitWise.Domain.DTO.Response;

public class FriendResponse
{
    public List<AcceptedFriendResponse>? AcceptedFriends { get; set; } = new List<AcceptedFriendResponse>();
    public List<PendingFriendResponse>? PendingFriends { get; set; } = new List<PendingFriendResponse>();
}

public class FriendBalancesSummary
{
    public Dictionary<Guid, Dictionary<Guid, decimal>> GroupBalancesPerGroup { get; set; } = new Dictionary<Guid, Dictionary<Guid, decimal>>();
    public Dictionary<Guid, decimal> OneToOneBalances { get; set; } = new Dictionary<Guid, decimal>();
}

public class FriendSettlementSummaryDto
{
    public List<SettleSummaryDto> GroupSettlements { get; set; } = new List<SettleSummaryDto>();
    public List<SettleSummaryDto> OneToOneSettlements { get; set; } = new List<SettleSummaryDto>();
}

public class PendingFriendResponse
{
    public Guid FromId { get; set; }
    public string FromName { get; set; } = null!;
    public Guid ToId { get; set; }
    public string ToName { get; set; } = null!;
}

public class AcceptedFriendResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? LastActivityDescription { get; set; }
    public decimal OweLentAmount { get; set; }
}