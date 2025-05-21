namespace SplitWise.Domain.DTO.Response;

public class GetExpenseByGroupId
{
    public Guid Id { get; set; }
    public string Description { get; set; } = null!;
    public string PayerName { get; set; } = null!;
    public decimal? Amount { get; set; }
    public decimal? OweLentAmount { get; set; }
    public decimal? OweLentAmountOverall { get; set; }
    public DateTime? Date { get; set; }

}
