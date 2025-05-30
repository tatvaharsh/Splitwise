import { Component, EventEmitter, Inject, Input, Output } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';
import { ExpenseService } from '../../services/expense.service';
import { SettleUpSummary } from '../../models/expense.model';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { SettleUpComponent } from '../settle-up/settle-up.component';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-settle-up-dialog',
  standalone: true,
  imports: [MatDialogModule, CommonModule, MatListModule, MatIconModule, FormsModule],
  templateUrl: './settle-up-dialog.component.html',
  styleUrl: './settle-up-dialog.component.css'
})
export class SettleUpDialogComponent {
  @Input() isOpen = false
  @Input() groupId!: string;
  @Input() friendId!: string;
  @Output() closeDialog = new EventEmitter<void>()
  balances: SettleUpSummary[] = [];
  constructor(
    private settleupservice : ExpenseService,
  ) {}
  selectedBalance: SettleUpSummary | null = null
  paymentAmount = ""

  ngOnChanges(): void {
    if(this.groupId){
      this.settleupservice.getSettleUpSummary(this.groupId).subscribe({
        next: (res) => {
          this.balances = res.content;
        }
    })
    }
    if(this.friendId) {
      this.settleupservice.getSettleUpSummaryByFriend(this.friendId).subscribe({
        next: (res) => {
          this.balances = res.content;
        }
      });
    }

  }
  handleBalanceSelect(balance: SettleUpSummary): void {
    this.selectedBalance = balance
    this.paymentAmount = balance.amount.toFixed(2)
  }

  handleBack(): void {
    this.selectedBalance = null
    this.paymentAmount = ""
  }

  handleRecordPayment(): void {
    const payload = {
      payerId: this.selectedBalance?.payerId,
      receiverId: this.selectedBalance?.receiverId,
      amount: this.selectedBalance?.amount,
      groupId: this.groupId
    };
  
    this.settleupservice.SettleUpGroup(payload).subscribe({
      next: (response) => {
        this.onClose(); // Close dialog or reset form
      }
    });
  }
  

  onClose(): void {
    this.selectedBalance = null
    this.paymentAmount = ""
    this.closeDialog.emit()
  }

  formatCurrency(amount: number): string {
    return `₹${amount.toLocaleString("en-IN", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })}`
  }

  getInitials(name: string): string {
    return name
      .split(" ")
      .map((n) => n[0])
      .join("")
  }
}