import type { User } from "./user.model"

export interface Friend extends User {
  balance: number // Positive means user is owed, negative means user owes
  lastActivity?: {
    description: string
    amount: number
    date: Date
  }
}
