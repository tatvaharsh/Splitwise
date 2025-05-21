export interface Group {
  id: string
  groupname: string
  autoLogo?: string
  totalMember: Number
  members?: Member[];
}

export interface Member {
    id: string;
    name: string;
}

export interface getExpensesByGroupId{
  id:string;
  description: string;
  amount: number;
  date: Date;
  payerName:string;
  oweLentAmount:number;
  oweLentAmountOverall:number;
}