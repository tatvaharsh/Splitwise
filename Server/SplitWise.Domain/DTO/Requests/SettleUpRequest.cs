namespace SplitWise.Domain.DTO.Requests;

public class SettleUpRequest
{
    public Guid PayerId { get; set; }
    public Guid ReceiverId { get; set; }
    public decimal Amount { get; set; }
    public Guid? GroupId { get; set; } 
}
