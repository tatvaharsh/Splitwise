import { Injectable } from "@angular/core"
import { BehaviorSubject, type Observable } from "rxjs"
import type { User } from "../models/user.model"

@Injectable({
  providedIn: "root",
})
export class UserService {
  private currentUserSubject = new BehaviorSubject<User>({
    id: "user1",
    name: "John Doe",
    email: "john.doe@example.com",
    phone: "+1234567890",
    profilePic: "assets/profile.jpg",
  })

  currentUser$: Observable<User> = this.currentUserSubject.asObservable()

  constructor() {}

  getCurrentUser(): User {
    return this.currentUserSubject.value
  }

  updateUser(user: User): void {
    this.currentUserSubject.next(user)
  }
}
