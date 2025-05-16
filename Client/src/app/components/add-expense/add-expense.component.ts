import { Component, Inject, type OnInit, ViewChild } from "@angular/core"
import { MatStepper } from "@angular/material/stepper"
import { FormBuilder, type FormGroup, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms"
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog"
import type { Friend } from "../../models/friend.model"
import type { User } from "../../models/user.model"
import { ExpenseService } from "../../services/expense.service"
import { UserService } from "../../services/user.service"
import { FriendService } from "../../services/friend.service"
import { ActivityService } from "../../services/activity.service"
import { Group1, GroupService } from "../../services/group.service"
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule } from '@angular/material/radio';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { CommonModule } from "@angular/common"

@Component({
  selector: "app-add-expense",
  templateUrl: "./add-expense.component.html",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatIconModule,
    MatButtonModule,
    MatRadioModule,
    MatCheckboxModule,
    MatButtonToggleModule
  ],
  styleUrls: ["./add-expense.component.scss"],
})
export class AddExpenseComponent implements OnInit {
  @ViewChild("stepper") stepper!: MatStepper

  expenseForm: FormGroup
  splitMethod = "equal"
  currentStep = 0

  // Data
  groups: Group1[] = []
  friends: Friend[] = []
  currentUser: User
  participants: User[] = []

  // For context when opened from a specific group or friend
  group?: Group1
  friend?: Friend

  // Split data
  splitMethods = [
    { value: "equal", label: "Equally", icon: "money_off" },
    { value: "unequal", label: "Unequally", icon: "shopping_bag" },
    { value: "percent", label: "By percentages", icon: "pie_chart" },
  ]

  totalAmount = 0
  splitAmounts: { [key: string]: number } = {}
  splitPercentages: { [key: string]: number } = {}
  selectedParticipants: { [key: string]: boolean } = {};

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<AddExpenseComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private readonly expenseService: ExpenseService,
    private readonly userService: UserService,
    private readonly groupService: GroupService,
    private readonly friendService: FriendService,
    private readonly activityService: ActivityService
  ) {
    this.currentUser = this.userService.getCurrentUser();
    
    if (data) {
      this.group = data.group;
      this.friend = data.friend;
    }
    
    this.expenseForm = this.fb.group({
      description: ['', Validators.required],
      amount: ['', [Validators.required, Validators.min(0.01)]],
      paidBy: [this.currentUser.id, Validators.required],
      splitMethod: ['equal', Validators.required],
      date: [new Date(), Validators.required],
      notes: [''],
      groupId: [this.group?.id || ''],
      friendId: [this.friend?.id || '']
    });
    
    // Load groups and friends
    this.groupService.getGroupss().subscribe(groups => {
      this.groups = groups;
    });
    
    this.friendService.getFriends().subscribe(friends => {
      this.friends = friends;
    });
    
    // Initialize participants
    this.initializeParticipants();
  }

  ngOnInit(): void {}

  initializeParticipants() {
    if (this.group) {
      this.participants = [...this.group.members]
    } else if (this.friend) {
      this.participants = [this.currentUser, this.friend]
    } else {
      this.participants = [this.currentUser]
    }

    // Initialize selected participants and split amounts
    this.participants.forEach((p) => {
      this.selectedParticipants[p.id] = true
      this.splitAmounts[p.id] = 0
      this.splitPercentages[p.id] = 0
    })
  }

  onAmountChange() {
    this.totalAmount = Number.parseFloat(this.expenseForm.get("amount")?.value) || 0
    this.updateSplitAmounts()
  }

  updateSplitAmounts() {
    const selectedCount = Object.values(this.selectedParticipants).filter((v) => v).length
    if (selectedCount === 0) return

    const splitMethod = this.expenseForm.get("splitMethod")?.value

    if (splitMethod === "equal") {
      const equalShare = this.totalAmount / selectedCount

      for (const id in this.selectedParticipants) {
        this.splitAmounts[id] = this.selectedParticipants[id] ? equalShare : 0
        this.splitPercentages[id] = this.selectedParticipants[id] ? 100 / selectedCount : 0
      }
    } else if (splitMethod === "percent") {
      for (const id in this.splitPercentages) {
        this.splitAmounts[id] = (this.splitPercentages[id] / 100) * this.totalAmount
      }
    }
  }

  toggleParticipant(id: string) {
    this.selectedParticipants[id] = !this.selectedParticipants[id]
    this.updateSplitAmounts()
  }

  changeSplitMethod(method: string) {
    this.expenseForm.get("splitMethod")?.setValue(method)
    this.splitMethod = method
    this.updateSplitAmounts()
  }

  getTotalSplitAmount(): number {
    return Object.values(this.splitAmounts).reduce((sum, amount) => sum + amount, 0)
  }

  getTotalSplitPercentage(): number {
    return Object.values(this.splitPercentages).reduce((sum, percent) => sum + percent, 0)
  }

  getRemainingAmount(): number {
    return this.totalAmount - this.getTotalSplitAmount()
  }

  getRemainingPercentage(): number {
    return 100 - this.getTotalSplitPercentage()
  }

  getSelectedParticipantsCount(): number {
    return Object.values(this.selectedParticipants).filter((v) => v).length
  }

  getParticipantName(user: User): string {
    return user.id === this.currentUser.id ? "You" : user.name
  }

  setPayer(userId: string) {
    this.expenseForm.get("paidBy")?.setValue(userId)
  }

  nextStep() {
    this.currentStep++
    if (this.stepper) {
      this.stepper.next()
    }
  }

  previousStep() {
    this.currentStep--
    if (this.stepper) {
      this.stepper.previous()
    }
  }

  onSubmit(): void {
    if (this.expenseForm.invalid) return

    const formValue = this.expenseForm.value

    // Find payer
    const payer = this.participants.find((p) => p.id === formValue.paidBy) || this.currentUser

    // Create split array based on selected participants and amounts
    const splitBetween = []
    for (const id in this.selectedParticipants) {
      if (this.selectedParticipants[id]) {
        const user = this.participants.find((p) => p.id === id)
        if (user) {
          splitBetween.push({
            user,
            amount: this.splitAmounts[id],
          })
        }
      }
    }

    // Create expense object
    const expense = {
      description: formValue.description,
      amount: this.totalAmount,
      date: formValue.date,
      paidBy: payer,
      splitBetween,
      groupId: formValue.groupId || undefined,
      notes: formValue.notes || undefined,
    }

    // Add expense
    this.expenseService.addExpense(expense)

    // Add activity
    let activityDescription = ""
    if (payer.id === this.currentUser.id) {
      activityDescription = `You paid ₹${this.totalAmount} for ${formValue.description}`
    } else {
      activityDescription = `${payer.name} paid ₹${this.totalAmount} for ${formValue.description}`
    }

    if (formValue.groupId) {
      const group = this.groups.find((g) => g.id === formValue.groupId)
      if (group) {
        activityDescription += ` in ${group.name}`
      }
    }

    this.activityService.addActivity({
      type: "expense",
      description: activityDescription,
      amount: this.totalAmount,
      date: new Date(),
      groupId: formValue.groupId,
      groupName: this.groups.find((g) => g.id === formValue.groupId)?.name,
      friendId: formValue.friendId,
      friendName: this.friends.find((f) => f.id === formValue.friendId)?.name,
      icon: "receipt_long",
    })

    this.dialogRef.close()
  }

  onCancel(): void {
    this.dialogRef.close()
  }
}
