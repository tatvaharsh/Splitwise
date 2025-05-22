import type { User } from "./user.model"

export interface Expenses {
  id: string
  description: string
  amount: number
  date: Date
  paidBy: User
  splitBetween: {
    user: User
    amount: number
  }[]
  groupId?: string
  notes?: string
}

export interface Group {
  id: string
  name: string
  members: Member[];
}
export interface Member {
  id: string;
  name: string;
}

export interface ExpenseApiResponseContent {
  groups: Group[];
  friends: Member[];
}

export interface Split {
  userId: string;
  splitAmount: number;
  name:string;
  percent: number;
  selected:boolean;
}

export interface Expense {
  id?:string;
  description: string;
  amount: number;
  date: string;
  groupId: string|null; 
  paidById: string;
  splits: Split[];
}

export interface ActivitySplitRequest {
  userId: string;
  splitAmount: number;
}

export interface UpdateActivityRequest {
  id: string;
  description: string;
  paidById: string;
  groupId?: string | null;
  amount: number;
  date: Date; 
  splits: ActivitySplitRequest[];
}
