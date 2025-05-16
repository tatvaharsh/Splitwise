import { Component, Inject, type OnInit } from "@angular/core"
import { FormBuilder, type FormGroup, ReactiveFormsModule, Validators } from "@angular/forms"
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog"
import type { Group } from "../../models/group.model"
import type { Friend } from "../../models/friend.model"
import { FriendService } from "../../services/friend.service"
import { ActivityService } from "../../services/activity.service"
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core'; // For native date adapter
import { MatIconModule } from '@angular/material/icon'; // For matSuffix icon toggle
import { CommonModule } from "@angular/common"
@Component({
  selector: "app-settle-up",
  templateUrl: "./settle-up.component.html",
  standalone: true,
  imports: [MatFormFieldModule, MatSelectModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatIconModule, CommonModule, ReactiveFormsModule],
  styleUrls: ["./settle-up.component.scss"],
})
export class SettleUpComponent implements OnInit {
  settleForm: FormGroup
  group?: Group
  friend?: Friend
  paymentMethods = ["cash", "bank transfer", "UPI", "other"];

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<SettleUpComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private friendService: FriendService,
    private activityService: ActivityService
  ) {
    this.data = data;
    // Initialize with data if provided
    if (data) {
      this.group = data.group;
      this.friend = data.friend;
    }
    
    let amount = 0;
    if (this.friend) {
      amount = Math.abs(this.friend.balance);
    }
    
    this.settleForm = this.fb.group({
      amount: [amount, [Validators.required, Validators.min(0.01)]],
      method: ['cash', Validators.required],
      notes: ['']
    });
  }

  ngOnInit(): void {}

  onSubmit(): void {
    if (this.settleForm.invalid) return

    const formValue = this.settleForm.value

    if (this.friend) {
      // Settle up with friend
      this.friendService.settleUp(this.friend.id)

      // Add activity
      this.activityService.addActivity({
        type: "settlement",
        description: `You settled ₹${formValue.amount} with ${this.friend.name}`,
        amount: formValue.amount,
        date: new Date(),
        friendId: this.friend.id,
        friendName: this.friend.name,
        icon: "payments",
      })
    }

    this.dialogRef.close()
  }

  onCancel(): void {
    this.dialogRef.close()
  }

  getSettlementText(): string {
    if (this.friend) {
      if (this.friend.balance > 0) {
        return `${this.friend.name} owes you ₹${Math.abs(this.friend.balance)}`
      } else if (this.friend.balance < 0) {
        return `You owe ${this.friend.name} ₹${Math.abs(this.friend.balance)}`
      }
    }
    return ""
  }
}
