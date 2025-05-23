namespace SplitWise.Domain.DTO.Response;

public class ActivityResponse
{
    public Guid Id { get; set; }
    public string Description { get; set; } = null!;
    public DateTime? Date { get; set; }
    public decimal? Amount { get; set; }
}