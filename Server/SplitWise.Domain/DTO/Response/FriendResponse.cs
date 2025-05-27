namespace SplitWise.Domain.DTO.Response;

public class FriendResponse
{
    public List<AcceptedFriendResponse>? AcceptedFriends { get; set; } = new List<AcceptedFriendResponse>();
    public List<PendingFriendResponse>? PendingFriends { get; set; } = new List<PendingFriendResponse>();
}

public class PendingFriendResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class AcceptedFriendResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? LastActivityDescription { get; set; }
    public decimal OweLentAmount { get; set; }
}