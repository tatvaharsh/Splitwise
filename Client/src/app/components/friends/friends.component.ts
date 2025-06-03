import { Component, type OnInit } from "@angular/core"
import type { Observable } from "rxjs"
import type { Friend, FriendResponse } from "../../models/friend.model"
import { AddFriendComponent } from "../add-friend/add-friend.component"
import { FriendService } from "../../services/friend.service"
import { Router } from "@angular/router"
import { MatDialog } from "@angular/material/dialog"
import { MatCardModule } from "@angular/material/card"
import { MatIconModule } from "@angular/material/icon"
import { CommonModule } from "@angular/common"
import { LocalStorageService } from "../../services/storage.service"
import { IJwtPayload } from "../../models/auth.model"

@Component({
  selector: "app-friends",
  templateUrl: "./friends.component.html",
  standalone: true,
    imports: [MatCardModule, MatIconModule, CommonModule],
  styleUrls: ["./friends.component.scss"],
})
export class FriendsComponent implements OnInit {
  friends!: FriendResponse
  currentUserId: string | null = null;
  constructor(
    private friendService: FriendService,
    private router: Router,
    private dialog: MatDialog,
    private storageService: LocalStorageService
  ) {

  }

  ngOnInit(): void {
    const tokenPayload: IJwtPayload | null = this.storageService.getDecodedToken();
    this.currentUserId = tokenPayload?.UserId || null;
    console.log("Current User ID:", this.currentUserId);
    this.friendService.getAllFriendDetail().subscribe((res) => {
      this.friends = res.content;
      console.log(this.friends);
    });
  }

  openFriend(friendId: string): void {
    this.router.navigate(["/friends", friendId])
  }

  openAddFriendDialog(): void {
    this.dialog.open(AddFriendComponent, {
      width: "500px",
      maxWidth: "95vw",
    })
  }

  getBalanceText(balance: number): string {
    if (balance > 0) {
      return `owes you ₹${Math.abs(balance)}`
    } else if (balance < 0) {
      return `you owe ₹${Math.abs(balance)}`
    } else {
      return "settled up"
    }
  }

  getBalanceClass(balance: number): string {
    if (balance > 0) {
      return "positive"
    } else if (balance < 0) {
      return "negative"
    } else {
      return "neutral"
    }
  }

  approveFriend(id: string) {
    this.friendService.approveFriend(id).subscribe(() => {
      this.friendService.getAllFriendDetail().subscribe((res) => {
        this.friends = res.content;
      });
    });
  }
  
  rejectFriend(id: string) {
    this.friendService.rejectFriend(id).subscribe(() => {
      this.friendService.getAllFriendDetail().subscribe((res) => {
        this.friends = res.content;
      });
    });
  }
}
