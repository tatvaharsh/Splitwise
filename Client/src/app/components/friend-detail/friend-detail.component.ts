import { Component, type OnInit } from "@angular/core"
import { type Observable, switchMap } from "rxjs"
import type { Friend, GetFriendResponse } from "../../models/friend.model"
import type { Expense, Expenses } from "../../models/expense.model"
import { AddExpenseComponent } from "../add-expense/add-expense.component"
import { FriendService } from "../../services/friend.service"
import { ExpenseService } from "../../services/expense.service"
import { ActivatedRoute, RouterModule } from "@angular/router"
import { MatDialog } from "@angular/material/dialog"
import { SettleUpComponent } from "../settle-up/settle-up.component"
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core'; // For native date adapter
import { CommonModule } from "@angular/common"
import { MatCardModule } from "@angular/material/card"
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { DeleteConfirmationDialogComponent } from "../../generic/delete-confirmation-dialog"
import { DeleteConfirmationService } from "../../services/DeleteConfirmationService"
@Component({
  selector: "app-friend-detail",
  templateUrl: "./friend-detail.component.html",
  imports: [MatFormFieldModule, MatSelectModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatIconModule, CommonModule ,MatCardModule, MatButtonModule,
    MatIconModule,
    MatMenuModule,
    RouterModule],
  standalone: true,
  styleUrls: ["./friend-detail.component.scss"],
})
export class FriendDetailComponent implements OnInit {
  friend !: GetFriendResponse
  Math: any

  constructor(
    private route: ActivatedRoute,
    private friendService: FriendService,
    private expenseService: ExpenseService,
    private dialog: MatDialog,
    
  ) {

  }

  ngOnInit(): void {
    console.log("ngOnInit called");
    this.route.paramMap.subscribe(params => {
      console.log("Inside paramMap subscription");
      const friendId = params.get("id") || "";
      this.friendService.getFriendById(friendId).subscribe(response => {
        console.log("Friend data received", response);
        this.friend = response.content;
      });
    });
  }
  

  openAddExpenseDialog(friend: GetFriendResponse): void {
    this.dialog.open(AddExpenseComponent, {
      width: "500px",
      maxWidth: "95vw",
      data: { friend },
    })
  }

  openSettleUpDialog(friend: GetFriendResponse): void {
    this.dialog.open(SettleUpComponent, {
      width: "500px",
      maxWidth: "95vw",
      data: { friend },
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

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    })
  }
}
