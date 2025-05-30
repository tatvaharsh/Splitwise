namespace SplitWise.Domain.DTO.Response;

public class SettleSummaryDto
{
    public Guid PayerId { get; set; }
    public string PayerName { get; set; } = null!;
    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; } = null!;
    public decimal Amount { get; set; }
}

public class FriendSettleSummaryDto
{
    public List<SettleSummaryDto> GroupSettlements { get; set; } = new List<SettleSummaryDto>();
    public List<SettleSummaryDto> DirectSettlements { get; set; } = new List<SettleSummaryDto>();
}