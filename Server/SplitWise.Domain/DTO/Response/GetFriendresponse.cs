namespace SplitWise.Domain.DTO.Response;

public class GetFriendresponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public List<GroupItemResponse>? Expenses { get; set; } = new();
    public decimal OweLentAmountOverall { get; set; }
}
