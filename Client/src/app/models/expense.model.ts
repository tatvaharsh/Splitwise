import type { User } from "./user.model"

export interface Expense {
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
