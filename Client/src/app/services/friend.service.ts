import { Injectable } from "@angular/core"
import { BehaviorSubject, type Observable, of } from "rxjs"
import type { Friend } from "../models/friend.model"
import { HttpClient } from "@angular/common/http";
import { IResponse } from "../generic/response";
import { Member } from "../models/expense.model";

@Injectable({
  providedIn: "root",
})
export class FriendService {
  private apiUrl = `http://localhost:5158/api/Friend/`;
  constructor(private http: HttpClient) { } 

  getAvailableUsers(id:string): Observable<IResponse<Member[]>> {
    return this.http.get<IResponse<Member[]>>(
      `${this.apiUrl}get/${id}`
    );
  }

  addMemberToGroup(userId: string, groupId: string): Observable<IResponse<null>> {
    return this.http.post<IResponse<null>>(`${this.apiUrl}add/${userId}/${groupId}`, {});
  }

  deleteMember(id: string, GroupId:string): Observable<IResponse<null>> {
    return this.http.delete<IResponse<null>>(`${this.apiUrl}delete/${id}/${GroupId}`);
  }

  checkOutstanding(memberId: string, groupId: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}check-outstanding?memberId=${memberId}&groupId=${groupId}`);
  }
  
  private mockFriends: Friend[] = [
    {
      id: "friend1",
      name: "Jane Smith",
      email: "jane@example.com",
      phone: "+1987654321",
      profilePic: "assets/user2.jpg",
      balance: 500, // User is owed 500
      lastActivity: {
        description: "Dinner",
        amount: 500,
        date: new Date("2023-03-01"),
      },
    },
    {
      id: "friend2",
      name: "Mike Johnson",
      email: "mike@example.com",
      profilePic: "assets/user3.jpg",
      balance: -200, // User owes 200
      lastActivity: {
        description: "Movie tickets",
        amount: 400,
        date: new Date("2023-02-15"),
      },
    },
    {
      id: "friend3",
      name: "Sarah Williams",
      email: "sarah@example.com",
      profilePic: "assets/user4.jpg",
      balance: 150,
      lastActivity: {
        description: "Groceries",
        amount: 300,
        date: new Date("2023-02-28"),
      },
    },
    {
      id: "friend4",
      name: "Alex Brown",
      email: "alex@example.com",
      profilePic: "assets/user5.jpg",
      balance: -350,
      lastActivity: {
        description: "Lunch",
        amount: 350,
        date: new Date("2023-03-02"),
      },
    },
  ]

  private friendsSubject = new BehaviorSubject<Friend[]>(this.mockFriends)
  friends$: Observable<Friend[]> = this.friendsSubject.asObservable()

  getFriends(): Observable<Friend[]> {
    return this.friends$
  }

  getFriendById(id: string): Observable<Friend | undefined> {
    const friend = this.mockFriends.find((f) => f.id === id)
    return of(friend)
  }

  addFriend(friend: Omit<Friend, "id" | "balance">): void {
    const newFriend: Friend = {
      ...friend,
      id: `friend${this.mockFriends.length + 1}`,
      balance: 0,
    }

    this.mockFriends.push(newFriend)
    this.friendsSubject.next([...this.mockFriends])
  }

  updateFriendBalance(friendId: string, amount: number): void {
    const index = this.mockFriends.findIndex((f) => f.id === friendId)
    if (index !== -1) {
      this.mockFriends[index].balance += amount
      this.friendsSubject.next([...this.mockFriends])
    }
  }

  settleUp(friendId: string): void {
    const index = this.mockFriends.findIndex((f) => f.id === friendId)
    if (index !== -1) {
      this.mockFriends[index].balance = 0
      this.friendsSubject.next([...this.mockFriends])
    }
  }
}
