export interface UserBalanceDetail {
    userId: string;
    userName: string | null; 
    balance: number;
  }
  
  export interface GroupBalanceDetail {
    groupId: string;
    groupName: string | null;
    balances: UserBalanceDetail[];
  }
  
  export interface TransactionDetail {
    id: string;
    description: string;
    amount: number;
    payerId: string | null;
    payerName: string | null;
    receiverId: string | null;
    receiverName: string | null;
    groupId: string | null;
    groupName: string | null;
    isGroupTransaction: boolean; 
  }
  
  export interface SettleSummary {
    payerId: string;
    payerName: string | null;
    receiverId: string;
    receiverName: string | null;
    amount: number;
    groupId: string | null;
  }
  
  export interface FriendSettlementTransparency {
    initialOneToOneActivityBalances: UserBalanceDetail[];
    initialGroupBalances: GroupBalanceDetail[];
    relevantTransactions: TransactionDetail[];
    finalOneToOneActivityBalances: UserBalanceDetail[];
    finalGroupBalances: GroupBalanceDetail[];
    calculatedGroupSettlements: SettleSummary[];
    calculatedOneToOneSettlements: SettleSummary[];
  }
  export interface TransactionDto {
    id: string; // Guid maps to string in TS
    payerId: string | null;
    receiverId: string | null;
    amount: number;
    date: string; // DateTime maps to string
    description: string;
    isGroupExpense: boolean; 
  }
  
  export interface SimplifiedSettlementDto {
    payerId: string;
    payerName: string;
    receiverId: string;
    receiverName: string;
    amount: number;
  }
  
  export interface SettlementCalculationStepDto {
    description: string;
    balancesAfterStep: { [key: string]: number }; // Dictionary<Guid, decimal>
    settlementDetail: SimplifiedSettlementDto | null;
    creditorsInThisStep: string[];
    debtorsInThisStep: string[];
  }
  
  export interface MemberAggregationDetailDto {
    memberId: string;
    memberName: string;
    totalOwedToThem: number;
    totalTheyOweOthers: number;
    calculatedNetBalance: number;
    actualServiceNetBalance: number;
    balancesMatch: boolean;
  }
  
  export interface SettleSummaryExplanationResponseDto {
    groupId: string;
    groupName: string;
    memberNames: { [key: string]: string };
    initialNetBalancesFromActivities: { [key: string]: number };
    initialDetailedDebtsFromActivities: { [key: string]: number }; // Dictionary<string, decimal>
    relevantTransactionsConsidered: TransactionDto[];
    balancesAfterAllAdjustments: { [key: string]: number };
    calculationSteps: SettlementCalculationStepDto[];
    memberAggregationDetails: MemberAggregationDetailDto[]; // The new property
    finalSimplifiedSettlements: SimplifiedSettlementDto[];
    finalTransactionCount: number;
  }