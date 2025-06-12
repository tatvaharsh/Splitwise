
public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid? PayerId { get; set; }
    public Guid? ReceiverId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public bool IsGroupExpense { get; set; } // Make sure this is populated from your Transaction entity
     // Make sure this is populated from your Transaction entity
}

public class SimplifiedSettlementDto
{
    public Guid PayerId { get; set; }
    public string PayerName { get; set; }
    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; }
    public decimal Amount { get; set; }
}

public class SettlementCalculationStepDto
{
    public string Description { get; set; }
    public Dictionary<Guid, decimal> BalancesAfterStep { get; set; } = new Dictionary<Guid, decimal>();
    public SimplifiedSettlementDto SettlementDetail { get; set; } // Nullable, only present if this step is a settlement
    public List<string> CreditorsInThisStep { get; set; } = new List<string>(); // For the settlement step explanation
    public List<string> DebtorsInThisStep { get; set; } = new List<string>(); // For the settlement step explanation
}

public class SettleSummaryExplanationResponseDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; }

    public Dictionary<Guid, string> MemberNames { get; set; }
    public Dictionary<Guid, decimal> InitialNetBalancesFromActivities { get; set; }
    public Dictionary<string, decimal> InitialDetailedDebtsFromActivities { get; set; }
    public List<TransactionDto> RelevantTransactionsConsidered { get; set; }
    public Dictionary<Guid, decimal> BalancesAfterAllAdjustments { get; set; }
    public List<SettlementCalculationStepDto> CalculationSteps { get; set; }

    // New property to hold the aggregation details
    public List<MemberAggregationDetailDto> MemberAggregationDetails { get; set; }

    public List<SimplifiedSettlementDto> FinalSimplifiedSettlements { get; set; }
    public int FinalTransactionCount { get; set; }
}
public class MemberAggregationDetailDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; }
    public decimal TotalOwedToThem { get; set; }
    public decimal TotalTheyOweOthers { get; set; }
    public decimal CalculatedNetBalance { get; set; }
    public decimal ActualServiceNetBalance { get; set; }
    public bool BalancesMatch { get; set; } // Indicates if CalculatedNetBalance == ActualServiceNetBalance
}