import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface SplitOption {
  id: string;
  name: string;
}

interface Participant {
  id: number;
  name: string;
  amount: number;
  percentage: number;
  shares: number;
}

@Component({
  selector: 'app-expense-form-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './expense-form-component.component.html',
  styleUrl: './expense-form-component.component.scss'
})
export class ExpenseFormComponentComponent implements OnInit {
  @Input() groups: any[] = [];
  @Input() friends: any[] = [];
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<any>();

  description: string = '';
  amount: number | null = null;
  paidBy: string = 'you';
  selectedGroup: string = '';
  date: string = new Date().toISOString().split('T')[0];
  
  splitOptions: SplitOption[] = [
    { id: 'equal', name: 'Equally' },
    { id: 'unequal', name: 'Unequally' },
    { id: 'percentage', name: 'By percentage' },
    { id: 'shares', name: 'By shares' }
  ];
  
  selectedSplitOption: string = 'equal';
  participants: Participant[] = [];

  constructor() { }

  ngOnInit(): void {
    // Initialize with default participants (current user)
    this.participants = [
      { id: 0, name: 'You', amount: 0, percentage: 100, shares: 1 }
    ];
  }

  onGroupChange(event: any): void {
    const value = event.target.value;
    this.selectedGroup = value;
    
    // Reset participants based on selected group or friend
    if (value.startsWith('group-')) {
      const groupId = parseInt(value.replace('group-', ''));
      const group = this.groups.find(g => g.id === groupId);
      if (group) {
        this.participants = group.members.map((name: string, index: number) => ({
          id: index,
          name,
          amount: 0,
          percentage: 100 / group.members.length,
          shares: 1
        }));
      }
    } else if (value.startsWith('friend-')) {
      const friendId = parseInt(value.replace('friend-', ''));
      const friend = this.friends.find(f => f.id === friendId);
      if (friend) {
        this.participants = [
          { id: 0, name: 'You', amount: 0, percentage: 50, shares: 1 },
          { id: 1, name: friend.name, amount: 0, percentage: 50, shares: 1 }
        ];
      }
    }
    
    this.updateAmounts();
  }

  onSplitOptionChange(option: string): void {
    this.selectedSplitOption = option;
    this.updateAmounts();
  }

  onPaidByChange(paidBy: string): void {
    this.paidBy = paidBy;
  }

  updateAmounts(): void {
    if (!this.amount) return;
    
    switch (this.selectedSplitOption) {
      case 'equal':
        const equalShare = this.amount / this.participants.length;
        this.participants.forEach(p => p.amount = equalShare);
        break;
      case 'percentage':
        this.participants.forEach(p => {
          p.amount = (this.amount || 0) * (p.percentage / 100);
        });
        break;
      case 'shares':
        const totalShares = this.participants.reduce((sum, p) => sum + p.shares, 0);
        this.participants.forEach(p => {
          p.amount = (this.amount || 0) * (p.shares / totalShares);
        });
        break;
      // For unequal, we don't auto-calculate - user enters specific amounts
    }
  }

  onAmountChange(): void {
    this.updateAmounts();
  }

  onParticipantAmountChange(participant: Participant): void {
    // For unequal split, we need to ensure the total matches the expense amount
    if (this.selectedSplitOption === 'unequal') {
      const total = this.participants.reduce((sum, p) => sum + p.amount, 0);
      if (total !== this.amount && this.amount !== null) {
        // Adjust the last participant's amount to make the total match
        const diff = this.amount - total;
        const lastParticipant = this.participants.find(p => p.id !== participant.id);
        if (lastParticipant) {
          lastParticipant.amount += diff;
        }
      }
    }
  }

  onSubmit(): void {
    if (!this.description || !this.amount) {
      return; // Basic validation
    }
    
    const expenseData = {
      description: this.description,
      amount: this.amount,
      paidBy: this.paidBy === 'you' ? 'You' : 'Someone else', // In a real app, you'd select the actual person
      date: new Date(this.date),
      group: this.selectedGroup.startsWith('group-') ? 
        this.groups.find(g => g.id === parseInt(this.selectedGroup.replace('group-', '')))?.name : undefined,
      participants: this.participants.map(p => p.name),
      splits: this.participants.map(p => ({
        name: p.name,
        amount: p.amount
      }))
    };
    
    this.save.emit(expenseData);
    this.close.emit();
  }

  onCancel(): void {
    this.close.emit();
  }
}