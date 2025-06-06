import { getExpensesByGroupId } from "./group.model"
import type { User } from "./user.model"

export interface Friend extends User {
  balance: number // Positive means user is owed, negative means user owes
  lastActivity?: {
    description: string
    amount: number
    date: Date
  }
}

export interface FriendResponse {
  acceptedFriends: acceptedFriends[];
  pendingFriends: pendingFriends[];
}
export interface acceptedFriends{
  id:string;
  name: string;
  lastActivityDescription:string;
  oweLentAmount:number;
}

export interface pendingFriends{
  fromId:string;
  fromName: string;
  toId:string;
  toName: string; 
}

export interface GetExpenseByGroupId {
  id: string;                     
  description: string;
  payerName: string;
  amount?: number | null;
  oweLentAmount?: number | null;
  oweLentAmountOverall?: number | null;
  date: string;         
}

export interface GetFriendResponse {
  id: string;                    
  name: string;
  expenses?: getExpensesByGroupId[]; 
  oweLentAmountOverall: number;
}

export interface AddFriendRequest {
  name: string;
  email: string;
}
