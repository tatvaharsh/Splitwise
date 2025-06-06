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
    public bool? IsSettlement { get; set; }


}

public class GroupItemResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } // "Expense" or "SettleUp"
    public string Description { get; set; }
    public string PayerName { get; set; }
    public string ReceiverName { get; set; } // Applicable mainly for "SettleUp" transactions
    public decimal Amount { get; set; } // Total amount of the expense or transaction
    public DateTime Date { get; set; } // Date of the expense or transaction
    public decimal OweLentAmount { get; set; } // The current user's individual owe/lent for *this specific item*
    public decimal OweLentAmountOverall { get; set; } // The current user's *overall* balance in the group after all items
    public DateTime OrderDate { get; set; } // Date of the expense or transaction

}
