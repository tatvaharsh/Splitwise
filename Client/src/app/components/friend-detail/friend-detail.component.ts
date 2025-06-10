import { Component, type OnInit } from "@angular/core"
import { type Observable, switchMap } from "rxjs"
import type { Friend, GetFriendResponse } from "../../models/friend.model"
import type { Expense, Expenses } from "../../models/expense.model"
import { AddExpenseComponent } from "../add-expense/add-expense.component"
import { FriendService } from "../../services/friend.service"
import { ExpenseService } from "../../services/expense.service"
import { ActivatedRoute, Router, RouterModule } from "@angular/router"
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
import { SettleUpDialogComponent } from "../settle-up-dialog/settle-up-dialog.component"
@Component({
  selector: "app-friend-detail",
  templateUrl: "./friend-detail.component.html",
  imports: [MatFormFieldModule, MatSelectModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatIconModule, CommonModule ,MatCardModule, MatButtonModule,
    MatIconModule,
    MatMenuModule,
    RouterModule,
    DeleteConfirmationDialogComponent, SettleUpDialogComponent],
  standalone: true,
  styleUrls: ["./friend-detail.component.scss"],
})
export class FriendDetailComponent implements OnInit {
  dialogState$ = this.deleteService.dialogState$;
  friend !: GetFriendResponse
  Math: any
  expenseId: string = '';
  isDialogOpen = false;
  friendId: string = '';
  
  constructor(
    private route: ActivatedRoute,
    private friendService: FriendService,
    private expenseService: ExpenseService,
    private dialog: MatDialog,
    private deleteService: DeleteConfirmationService,
    private router: Router,
  ) {
    
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const friendId = params.get("id") || "";
      this.friendService.getFriendById(friendId).subscribe(response => {
        this.friend = response.content;
        this.friendId = this.friend.id || '';
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
  
  openSettleUpDialog(friendId: string): void {
    this.isDialogOpen = true
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
  
  editExpense(id: string): void {
    this.expenseService.getExpenseById(id).subscribe(expense => {
      this.dialog.open(AddExpenseComponent, {
        width: "500px",
        maxWidth: "95vw",
        panelClass: "expense-dialog",
        data: {  expenseId: expense.content.id } // Pass data to the component
      });
    });
  }
  
  
  deleteexpense(id: string) {
    this.expenseId = id;
    this.deleteService.open({
      title: 'Confirm Delete',
      message: `Are you sure you want to delete this item?`
    });
  }
  
  deleteExpense(): void {
    this.expenseService.deleteExpense(this.expenseId).subscribe({
        next: () => {
          this.deleteService.close();
          this.router.navigate(['/friends']);
        } 
      });
  }

  cancelDelete(): void {
    this.deleteService.close();
  }
  
  closeDialog(): void {
    this.isDialogOpen = false
  }
  
  getAbsoluteValue(amount: number): number {  
    return Math.abs(amount);
  }

  navigateToTransparency(friendId: string) {
    this.router.navigate(["/transparency", friendId])

  }
}