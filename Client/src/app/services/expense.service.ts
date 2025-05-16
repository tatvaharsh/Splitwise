import { Injectable } from "@angular/core"
import { BehaviorSubject, type Observable, of } from "rxjs"
import type { Expense } from "../models/expense.model"
import { UserService } from "./user.service"
import { FriendService } from "./friend.service"

@Injectable({
  providedIn: "root",
})
export class ExpenseService {
  private mockExpenses: Expense[] = [
    {
      id: "exp1",
      description: "Dinner at Taj",
      amount: 1500,
      date: new Date("2023-02-20"),
      paidBy: {
        id: "user1",
        name: "John Doe",
        email: "john@example.com",
      },
      splitBetween: [
        {
          user: { id: "user1", name: "John Doe", email: "john@example.com" },
          amount: 500,
        },
        {
          user: { id: "user2", name: "Jane Smith", email: "jane@example.com" },
          amount: 500,
        },
        {
          user: { id: "user3", name: "Mike Johnson", email: "mike@example.com" },
          amount: 500,
        },
      ],
      groupId: "group1",
    },
    {
      id: "exp2",
      description: "Rent March",
      amount: 25000,
      date: new Date("2023-03-01"),
      paidBy: {
        id: "user1",
        name: "John Doe",
        email: "john@example.com",
      },
      splitBetween: [
        {
          user: { id: "user1", name: "John Doe", email: "john@example.com" },
          amount: 12500,
        },
        {
          user: { id: "user4", name: "Sarah Williams", email: "sarah@example.com" },
          amount: 12500,
        },
      ],
      groupId: "group2",
    },
    {
      id: "exp3",
      description: "Movie tickets",
      amount: 800,
      date: new Date("2023-02-15"),
      paidBy: {
        id: "user3",
        name: "Mike Johnson",
        email: "mike@example.com",
      },
      splitBetween: [
        {
          user: { id: "user1", name: "John Doe", email: "john@example.com" },
          amount: 200,
        },
        {
          user: { id: "user2", name: "Jane Smith", email: "jane@example.com" },
          amount: 200,
        },
        {
          user: { id: "user3", name: "Mike Johnson", email: "mike@example.com" },
          amount: 200,
        },
        {
          user: { id: "user4", name: "Sarah Williams", email: "sarah@example.com" },
          amount: 200,
        },
      ],
      groupId: "group3",
    },
  ]

  private expensesSubject = new BehaviorSubject<Expense[]>(this.mockExpenses)
  expenses$: Observable<Expense[]> = this.expensesSubject.asObservable()

  constructor(
    private userService: UserService,
    private friendService: FriendService,
  ) {}

  getExpenses(): Observable<Expense[]> {
    return this.expenses$
  }

  getExpensesByGroupId(groupId: string): Observable<Expense[]> {
    const expenses = this.mockExpenses.filter((e) => e.groupId === groupId)
    return of(expenses)
  }

  getExpensesByFriendId(friendId: string): Observable<Expense[]> {
    const currentUser = this.userService.getCurrentUser()
    const expenses = this.mockExpenses.filter(
      (e) =>
        e.splitBetween.some((split) => split.user.id === friendId) &&
        e.splitBetween.some((split) => split.user.id === currentUser.id),
    )
    return of(expenses)
  }

  addExpense(expense: Omit<Expense, "id">): void {
    const newExpense: Expense = {
      ...expense,
      id: `exp${this.mockExpenses.length + 1}`,
    }

    this.mockExpenses.push(newExpense)
    this.expensesSubject.next([...this.mockExpenses])

    // Update friend balances if this is a direct expense between friends
    if (!expense.groupId) {
      const currentUser = this.userService.getCurrentUser()
      expense.splitBetween.forEach((split) => {
        if (split.user.id !== currentUser.id) {
          // If current user paid, they are owed money
          if (expense.paidBy.id === currentUser.id) {
            this.friendService.updateFriendBalance(split.user.id, split.amount)
          }
          // If the friend paid, current user owes money
          else if (expense.paidBy.id === split.user.id) {
            this.friendService.updateFriendBalance(split.user.id, -split.amount)
          }
        }
      })
    }
  }
}
