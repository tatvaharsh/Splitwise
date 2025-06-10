using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SplitWise.Domain.DTO.Response
{
    public class UserBalanceDetailDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } // For displaying the user's name
        public decimal Balance { get; set; }
    }

    public class GroupBalanceDetailDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } // For displaying the group's name
    public List<UserBalanceDetailDto> Balances { get; set; }
}

// A simplified Transaction DTO for display purposes, focusing on relevant info
public class TransactionDetailDto
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public Guid? PayerId { get; set; }
    public string PayerName { get; set; } // For displaying the payer's name
    public Guid? ReceiverId { get; set; }
    public string ReceiverName { get; set; } // For displaying the receiver's name
    public Guid? GroupId { get; set; }
    public string GroupName { get; set; } // For displaying the group name if it's a group transaction
    public bool IsGroupTransaction => GroupId.HasValue;
}

// The main DTO for the new transparency API, containing all calculation steps
public class FriendSettlementTransparencyDto
{
    // Balances derived from activities *before* applying manual settlement transactions
    public List<UserBalanceDetailDto> InitialOneToOneActivityBalances { get; set; }
    public List<GroupBalanceDetailDto> InitialGroupBalances { get; set; }

    // List of transactions between the two friends (including group transactions) that affect settlement
    public List<TransactionDetailDto> RelevantTransactions { get; set; }

    // Balances after applying manual settlement transactions (these are the ones used for final settlement suggestions)
    public List<UserBalanceDetailDto> FinalOneToOneActivityBalances { get; set; }
    public List<GroupBalanceDetailDto> FinalGroupBalances { get; set; }

    // The calculated settlement suggestions (reusing your existing SettleSummaryDto)
    public List<SettleSummaryDto> CalculatedGroupSettlements { get; set; }
    public List<SettleSummaryDto> CalculatedOneToOneSettlements { get; set; }
}
}