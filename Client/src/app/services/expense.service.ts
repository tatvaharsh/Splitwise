import { Injectable } from "@angular/core"
import { BehaviorSubject, type Observable, of } from "rxjs"
import type { Expense, ExpenseApiResponseContent, Expenses, SettleUpSummary, UpdateActivityRequest } from "../models/expense.model"
import { UserService } from "./user.service"
import { FriendService } from "./friend.service"
import { HttpClient } from "@angular/common/http"
import { IResponse } from "../generic/response"
import { getExpensesByGroupId } from "../models/group.model"

@Injectable({
  providedIn: "root",
})
export class ExpenseService {
  private apiUrl = `http://localhost:5158/api/Expense/`;
  constructor(
    private http: HttpClient,
    private userService: UserService,
    private friendService: FriendService,
  ) {}

  getSettleUpSummaryByFriend(friendId: string): Observable<IResponse<SettleUpSummary[]>> {
    return this.http.get<IResponse<SettleUpSummary[]>>(
      `${this.apiUrl}settle-summary/friends/${friendId}`
    );
  }

  SettleUpGroup(data : any): Observable<IResponse<null>> {
    return this.http.post<IResponse<null>>(`${this.apiUrl}settle-up`, data);
  }

  getSettleUpSummary(groupId: string): Observable<IResponse<SettleUpSummary[]>> {
    return this.http.get<IResponse<SettleUpSummary[]>>(
      `${this.apiUrl}settle-summary/${groupId}`
    );
  }

  FetchDropDownList(): Observable<
    IResponse<ExpenseApiResponseContent>> {return this.http.get<IResponse<ExpenseApiResponseContent>>(
      `${this.apiUrl}list`
    );
  }

  saveExpense(Expense: Expense): Observable<IResponse<null>> {
    return this.http.post<IResponse<null>>(`${this.apiUrl}create`, Expense);
  }

  getExpensesByGroupId(groupId: string): Observable<IResponse<getExpensesByGroupId[]>> {
    return this.http.get<IResponse<getExpensesByGroupId[]>>(
      `${this.apiUrl}get/${groupId}`
    );
  }

  getExpenseById(id: string): Observable<IResponse<Expense>> {
    return this.http.get<IResponse<Expense>>(
      `${this.apiUrl}getbyexpenseid/${id}`
    );
  }
  
  updateExpense(id: string, expense: Expense): Observable<IResponse<null>> {
    return this.http.put<IResponse<null>>(`${this.apiUrl}edit/${id}`, expense);
  }
  
  deleteExpense(id: string): Observable<IResponse<null>> {
    return this.http.delete<IResponse<null>>(`${this.apiUrl}delete/${id}`);
  } 

  private mockExpenses: Expenses[] = [
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

  private expensesSubject = new BehaviorSubject<Expenses[]>(this.mockExpenses)
  expenses$: Observable<Expenses[]> = this.expensesSubject.asObservable()



  getExpenses(): Observable<Expenses[]> {
    return this.expenses$
  }

  // getExpensesByGroupId(groupId: string): Observable<Expenses[]> {
  //   const expenses = this.mockExpenses.filter((e) => e.groupId === groupId)
  //   return of(expenses)
  // }

  getExpensesByFriendId(friendId: string): Observable<Expenses[]> {
    const currentUser = this.userService.getCurrentUser()
    const expenses = this.mockExpenses.filter(
      (e) =>
        e.splitBetween.some((split) => split.user.id === friendId) &&
        e.splitBetween.some((split) => split.user.id === currentUser.id),
    )
    return of(expenses)
  }

  addExpense(expense: Omit<Expenses, "id">): void {
    const newExpense: Expenses = {
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
