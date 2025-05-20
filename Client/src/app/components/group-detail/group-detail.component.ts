import { Component, type OnInit } from "@angular/core"
import { type Observable, switchMap } from "rxjs"
import type { getExpensesByGroupId, Group } from "../../models/group.model"
import type { Expense, Expenses } from "../../models/expense.model"
import { AddExpenseComponent } from "../add-expense/add-expense.component"
import { AddGroupMemberComponent } from "../add-group-member/add-group-member.component"
import { SettleUpComponent } from "../settle-up/settle-up.component"
import { Group1, GroupService } from "../../services/group.service"
import { ExpenseService } from "../../services/expense.service"
import { ActivatedRoute, Router } from "@angular/router"
import { MatDialog } from "@angular/material/dialog"
import { MatCardModule } from "@angular/material/card"
import { MatIconModule } from "@angular/material/icon"
import { CommonModule } from "@angular/common"
import { MatTabsModule } from '@angular/material/tabs';
import { MatMenu } from "@angular/material/menu"
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { AddGroupComponent } from "../add-group/add-group.component"
import { DeleteConfirmationDialogComponent } from "../../generic/delete-confirmation-dialog"
import { DeleteConfirmationService } from "../../services/DeleteConfirmationService"

@Component({
  selector: "app-group-detail",
  templateUrl: "./group-detail.component.html",
  imports: [MatCardModule, MatIconModule, CommonModule, MatTabsModule, MatButtonModule, MatMenuModule, DeleteConfirmationDialogComponent],
  standalone: true,
  styleUrls: ["./group-detail.component.scss"],
})
export class GroupDetailComponent implements OnInit {
  dialogState$ = this.deleteService.dialogState$;
  group!: Group;
  expenses: getExpensesByGroupId[] = [];
  activeTab = 0
  
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private groupService: GroupService,
    private expenseService: ExpenseService,
    private dialog: MatDialog,
    private deleteService: DeleteConfirmationService,
  ) {
    this.route.paramMap.subscribe(params => {
        const groupId = params.get("id") || "";
        this.groupService.getGroupById(groupId).subscribe(response => {
          this.group = response.content;
        });
      });
      this.route.paramMap.pipe(
        switchMap((params) => {
          const groupId = params.get("id") || "";
          return this.expenseService.getExpensesByGroupId(groupId);
        })
      ).subscribe(response => {
        this.expenses = response.content;
      });
    }

  ngOnInit(): void {}

  openAddExpenseDialog(group: Group): void {
    this.dialog.open(AddExpenseComponent, {
      width: "500px",
      maxWidth: "95vw",
      data: { group },
    })
  }

  openAddMemberDialog(group: Group): void {
    this.dialog.open(AddGroupMemberComponent, {
      width: "500px",
      maxWidth: "95vw",
      data: { groupId: group.id },
    })
  }

  openSettleUpDialog(group: Group): void {
    this.dialog.open(SettleUpComponent, {
      width: "500px",
      maxWidth: "95vw",
      data: { group },
    })
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    })
  }

  removeMember(memberId: string) {
    if (confirm('Are you sure you want to remove this member?')) {
    //   this.groupService.removeMemberFromGroup(this.group.id, memberId).subscribe({
    //     next: (res) => {
    //       // Remove member from local group.members array after successful API call
    //       this.group.members = this.group.members?.filter(m => m.id !== memberId) || [];
    //       this.group.totalMember = this.group.members.length;
    //       this.snackBar.open('Member removed successfully', 'Close', { duration: 3000 });
    //     },
    //     error: (err) => {
    //       console.error(err);
    //       this.snackBar.open('Failed to remove member', 'Close', { duration: 3000 });
    //     }
    //   });
    }
  }
  editGroup(group: any): void {
    this.dialog.open(AddGroupComponent, {
      width: '500px',
      maxWidth: '95vw',
      data: group  // Pass group object or just { id: group.id }
    });
  }
  confirmDelete(id: string): void {
    this.group.id = id;
    this.deleteService.open({
      title: 'Confirm Delete',
      message: `Are you sure you want to delete this item?`
    });
  }

  deleteExpense(): void {
    this.groupService.deleteGroup(this.group.id).subscribe({
      next: () => {
        this.deleteService.close();
        this.router.navigate(['/groups']);
      }
    });
  }

  cancelDelete(): void {
    this.deleteService.close();
  }
}
