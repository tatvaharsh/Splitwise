import { Component, type OnInit } from "@angular/core"
import { type Observable, switchMap } from "rxjs"
import type { getExpensesByGroupId, Group } from "../../models/group.model"
import type { Expense, Expenses } from "../../models/expense.model"
import { AddExpenseComponent } from "../add-expense/add-expense.component"
import { AddGroupMemberComponent } from "../add-group-member/add-group-member.component"
import { SettleUpComponent } from "../settle-up/settle-up.component"
import { Group1, GroupService } from "../../services/group.service"
import { ExpenseService } from "../../services/expense.service"
import { ActivatedRoute, Router, RouterModule } from "@angular/router"
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
import { FriendService } from "../../services/friend.service"

@Component({
  selector: "app-group-detail",
  templateUrl: "./group-detail.component.html",
  imports: [MatCardModule, MatIconModule, CommonModule, MatTabsModule, MatButtonModule, MatMenuModule, DeleteConfirmationDialogComponent, RouterModule],
  standalone: true,
  styleUrls: ["./group-detail.component.scss"],
})
export class GroupDetailComponent implements OnInit {
  dialogState$ = this.deleteService.dialogState$;
  group!: Group;
  expenses: getExpensesByGroupId[] = [];
  activeTab = 0
  amount: number = 0
  memberId: string = '';
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private groupService: GroupService,
    private expenseService: ExpenseService,
    private friendService: FriendService,
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
        this.amount = this.expenses[0].oweLentAmountOverall ?? 0;
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

  removeMember(memberId: string, groupId: string): void {
    this.memberId = memberId;
    this.deleteService.open({
      title: 'Confirm Delete',
      message: `Are you sure you want to delete this item?`
    });

    
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
    if (this.group.id && this.memberId) {
      this.friendService.checkOutstanding(this.memberId, this.group.id).subscribe({
        next: (hasOutstanding) => {
          if (!hasOutstanding) {
            this.friendService.deleteMember(this.memberId, this.group.id).subscribe({
              next: () => {
                this.deleteService.close();
                this.router.navigate(['/groups']);
              }
            });
          } else {
            alert('Cannot delete member. They have outstanding balances in this group.');
            this.deleteService.close();
          }
        }
      });
    }
    
    else{
      this.groupService.deleteGroup(this.group.id).subscribe({
        next: () => {
          this.deleteService.close();
          this.router.navigate(['/groups']);
        }
      });
    }
  }

  cancelDelete(): void {
    this.deleteService.close();
  }

  // deleteMember(): void {
  //   this.friendService.deleteMember(this.memberId, this.group.id).subscribe({
  //     next: () => {
  //       this.deleteService.close();
  //       this.router.navigate(['/groups']);
  //     }
  //   });
  // }

  // cancelDeleteMember(): void {
  //   this.deleteService.close();
  // }

  getAbsoluteValue(amount: number): number {  
    return Math.abs(amount);
  }
}
