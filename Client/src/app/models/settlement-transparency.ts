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
  