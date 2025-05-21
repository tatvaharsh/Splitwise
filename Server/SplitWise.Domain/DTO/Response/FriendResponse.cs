namespace SplitWise.Domain.DTO.Response;

public class FriendResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? LastActivityDescription { get; set; }
    public decimal OweLentAmount { get; set; }
}
