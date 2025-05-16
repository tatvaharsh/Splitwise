namespace SplitWise.Domain.DTO.Requests;

public class CreateActivityRequest
{
    public string Description { get; set; } = null!;
    public Guid PaidById { get; set; }
    public Guid? GroupId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public List<ActivitySplitRequest> Splits { get; set; } = new();
}

public class ActivitySplitRequest
{
    public Guid UserId { get; set; }
    public decimal SplitAmount { get; set; }
}

public class UpdateActivityRequest
{
    public Guid Id { get; set; }
    public string Description { get; set; } = null!;
    public Guid PaidById { get; set; }
    public Guid? GroupId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public List<ActivitySplitRequest> Splits { get; set; } = new();
}