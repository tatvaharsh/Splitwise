import { CommonModule } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Expense, Group, Member } from '../../models/expense.model';
import { ExpenseService } from '../../services/expense.service';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { LocalStorageService } from '../../services/storage.service';

@Component({
  selector: 'app-add-expense',
  templateUrl: './add-expense.component.html',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['./add-expense.component.scss']
})
export class AddExpenseComponent implements OnInit {
  today: string = new Date().toISOString().split('T')[0];
  constructor(
    private expenseService: ExpenseService,
    private storageService: LocalStorageService, 
    private dialogRef: MatDialogRef<AddExpenseComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { expenseId?: string } 
  ) {}

  step = 1;
  splitType = 'equal';
  groups: Group[] = [];
  friends: Member[] = [];
  expense!: Expense;
  groupMembers: { id?: string; name: string }[] = [];
  remainingAmount: number = 0;
  expenseDateString: string = '';
  currentUserId: string = '';
  selectedType: 'group' | 'friend' | null = null;
  errors: { [key: string]: string } = {};

  ngOnInit(): void {
    const decoded = this.storageService.getDecodedToken();
    this.currentUserId = decoded?.UserId || '';

    this.FetchDropDownList();

    if (this.data?.expenseId) {
      this.loadExpense(this.data.expenseId);
    } else {
      this.initializeEmptyExpense();
    }
  }

  initializeEmptyExpense(): void {
    this.expense = {
      description: '',
      amount: 0,
      date: '',
      groupId: null,
      paidById: '',
      splits: []
    };
    this.errors = {};
  }

  loadExpense(expenseId: string): void {
    this.expenseService.getExpenseById(expenseId).subscribe({
      next: (res) => {
        const data = res.content;
        const dateObj = new Date(data.date);
        const yyyy = dateObj.getFullYear();
        const mm = (dateObj.getMonth() + 1).toString().padStart(2, '0');
        const dd = dateObj.getDate().toString().padStart(2, '0');
        data.date = `${yyyy}-${mm}-${dd}`;
        
        if (!data.groupId) {
          const otherMember = data.splits.find(
            (s: any) => s.userId !== this.currentUserId
          );
          if (otherMember) {
            data.groupId = otherMember.userId;
          }
        }
        this.expense = data;
        this.calculateEqualSplit();
      },
      error: (err) => console.error('Failed to load expense:', err)
    });
  }

  FetchDropDownList(): void {
    this.expenseService.FetchDropDownList().subscribe({
      next: (response) => {
        if (response?.content) {
          this.groups = response.content.groups || [];
          this.friends = response.content.friends || [];
        }
      },
      error: (error) => {
        console.error('Failed to fetch dropdown data:', error);
      }
    });
  }

  validateStep1(): boolean {
    this.errors = {};
    let isValid = true;

    if (!this.expense.description.trim()) {
      this.errors['description'] = 'Description is required';
      isValid = false;
    }

    if (!this.expense.amount || this.expense.amount <= 0) {
      this.errors['amount'] = 'Amount must be greater than 0';
      isValid = false;
    }

    if (!this.expense.date) {
      this.errors['date'] = 'Date is required';
      isValid = false;
    } else {
      const today = new Date();
      const selectedDate = new Date(this.expense.date);
      if (selectedDate > today) {
        this.errors['date'] = 'Date cannot be in the future';
        isValid = false;
      }
    }

    if (!this.expense.groupId) {
      this.errors['groupId'] = 'Please select a group or friend';
      isValid = false;
    }

    return isValid;
  }

  validateStep2(): boolean {
    this.errors = {};
    if (!this.expense.paidById) {
      this.errors['paidById'] = 'Please select who paid';
      return false;
    }
    return true;
  }

  validateStep3(): boolean {
    this.errors = {};
    
    if (this.splitType === 'unequal' || this.splitType === 'percent') {
      const totalSplit = this.expense.splits.reduce((sum, p) => sum + (p.splitAmount || 0), 0);
      if (Math.abs(totalSplit - this.expense.amount) > 0.01) {
        this.errors['split'] = 'Split amounts must equal total amount';
        return false;
      }
      
      if (this.splitType === 'percent') {
        const totalPercent = this.expense.splits.reduce((sum, p) => sum + (p.percent || 0), 0);
        if (Math.abs(totalPercent - 100) > 0.01) {
          this.errors['percent'] = 'Percentages must add up to 100%';
          return false;
        }
      }

      if (this.expense.splits.some(split => split.splitAmount < 0)) {
        this.errors['split'] = 'Split amounts cannot be negative';
        return false;
      }
    }

    return true;
  }

  validateField(field: string): void {
    delete this.errors[field];
    
    switch (field) {
      case 'description':
        if (!this.expense.description.trim()) {
          this.errors['description'] = 'Description is required';
        }
        break;
      case 'amount':
        if (!this.expense.amount || this.expense.amount <= 0) {
          this.errors['amount'] = 'Amount must be greater than 0';
        }
        break;
      case 'date':
        if (!this.expense.date) {
          this.errors['date'] = 'Date is required';
        } else {
          const today = new Date();
          const selectedDate = new Date(this.expense.date);
          if (selectedDate > today) {
            this.errors['date'] = 'Date cannot be in the future';
          }
        }
        break;
      case 'groupId':
        if (!this.expense.groupId) {
          this.errors['groupId'] = 'Please select a group or friend';
        }
        break;
      case 'paidById':
        if (!this.expense.paidById) {
          this.errors['paidById'] = 'Please select who paid';
        }
        break;
    }
  }

  goNext(): void {
    if (this.step === 1 && !this.validateStep1()) {
      return;
    }
    
    if (this.step === 2 && !this.validateStep2()) {
      return;
    }

    if (this.step === 1) {
      const selectedGroup = this.groups.find(group => group.id === this.expense.groupId);
      if (selectedGroup) {
        this.groupMembers = selectedGroup.members;
        this.selectedType = 'group';
      } else {
        const selectedFriend = this.friends.find(friend => friend.id === this.expense.groupId);
        if (selectedFriend) {
          this.groupMembers = [
            { id: this.currentUserId, name: 'You' },
            { id: selectedFriend.id, name: selectedFriend.name }
          ];
          this.selectedType = 'friend';
        }
        this.expense.groupId = this.selectedType === 'group' ? this.expense.groupId : null;
      }

      this.expense.splits = this.groupMembers.map(person => ({
        userId: person.id || '',
        name: person.name,
        splitAmount: 0,
        percent: 0,
        selected: true
      }));
    }

    this.step++;
    if (this.step === 3 && this.splitType === 'equal') {
      this.calculateEqualSplit();
      this.trackUnequalSplit();
    }
  }

  goBack(): void {
    if (this.step > 1) {
      this.step--;
      this.errors = {};
    }
  }

  setSplitType(type: string): void {
    this.splitType = type;
    this.errors = {};

    if (type === 'equal') {
      this.calculateEqualSplit();
      this.trackUnequalSplit();
    } else if (type === 'percent') {
      this.calculateFromPercent();
      this.trackUnequalSplit();
    }
  }

  calculateEqualSplit(): void {
    const splits = this.expense.splits;
    const totalAmount = this.expense.amount;
    const activeSplits = splits.length;

    if (activeSplits === 0 || totalAmount == null) return;

    const equalShare = parseFloat((totalAmount / activeSplits).toFixed(2));

    splits.forEach(p => {
      p.splitAmount = equalShare;
    });

    const totalAssigned = splits.reduce((sum, p) => sum + p.splitAmount, 0);
    const diff = parseFloat((totalAmount - totalAssigned).toFixed(2));
    if (diff !== 0) {
      splits[0].splitAmount += diff;
    }
  }

  calculateFromPercent(): void {
    this.expense.splits.forEach(s => {
      s.splitAmount = +(this.expense.amount * (s.percent || 0) / 100).toFixed(2);
    });
    this.trackUnequalSplit();
  }

  trackUnequalSplit(): void {
    const total = this.expense.amount;
    const assigned = this.expense.splits.reduce((sum, p) => sum + (p.splitAmount || 0), 0);
    this.remainingAmount = parseFloat((total - assigned).toFixed(2));
    this.validateStep3();
  }

  onSplitAmountChange(): void {
    this.trackUnequalSplit();
  }

  onPercentChange(): void {
    this.calculateFromPercent();
    this.trackUnequalSplit();
  }

  saveExpense(): void {
    if (!this.validateStep3()) {
      return;
    }

    if (this.expense.id) {
      this.expenseService.updateExpense(this.expense.id, this.expense).subscribe({
        next: (response) => {
          this.dialogRef.close(response);
          window.location.reload(); 
        }
      });
    } else {
      this.expenseService.saveExpense(this.expense).subscribe({
        next: (response) => {
          this.dialogRef.close(response);
          window.location.reload(); 
        },
        error: (err) => console.error('Failed to save expense:', err)
      });
    }
  }
}