namespace SplitWise.Domain.DTO.Response;

public class SettleSummaryDto
{
    public Guid PayerId { get; set; }
    public string PayerName { get; set; } = null!;
    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; } = null!;
    public decimal Amount { get; set; }
}
