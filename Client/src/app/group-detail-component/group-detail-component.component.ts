import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ExpenseFormComponentComponent } from '../expense-form-component/expense-form-component.component';
import { CommonModule } from '@angular/common';

interface Expense {
  id: number;
  description: string;
  amount: number;
  paidBy: string;
  date: Date;
  participants: string[];
  splits: { name: string; amount: number }[];
}

interface Balance {
  from: string;
  to: string;
  amount: number;
}

interface Member {
  name: string;
  totalPaid: number;
  totalOwed: number;
  netBalance: number;
}

@Component({
  selector: 'app-group-detail-component',
  standalone: true,
  imports: [ExpenseFormComponentComponent, CommonModule],
  templateUrl: './group-detail-component.component.html',
  styleUrl: './group-detail-component.component.scss'
})
export class GroupDetailComponentComponent implements OnInit {
  groupId: number = 0;
  groupName: string = '';
  members: Member[] = [];
  expenses: Expense[] = [];
  balances: Balance[] = [];
  isExpenseModalOpen: boolean = false;
  Math = Math; 
  constructor(private route: ActivatedRoute) { }

  get groupInfoArray() {
    return [{
      id: this.groupId,
      name: this.groupName,
      members: this.members.map(m => m.name)
    }];
  }
  

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.groupId = +params['id'];
      this.loadGroupData();
    });
  }

  loadGroupData(): void {
    // In a real app, this would be an API call
    // Mock data for demonstration
    this.groupName = 'Roommates';
    
    this.members = [
      { name: 'You', totalPaid: 350.50, totalOwed: 250.25, netBalance: 100.25 },
      { name: 'Alex', totalPaid: 220.75, totalOwed: 250.25, netBalance: -29.50 },
      { name: 'Jamie', totalPaid: 180.00, totalOwed: 250.25, netBalance: -70.25 }
    ];
    
    this.expenses = [
      {
        id: 1,
        description: 'Dinner at Italian Restaurant',
        amount: 120.00,
        paidBy: 'You',
        date: new Date('2023-05-10'),
        participants: ['You', 'Alex', 'Jamie'],
        splits: [
          { name: 'You', amount: 40.00 },
          { name: 'Alex', amount: 40.00 },
          { name: 'Jamie', amount: 40.00 }
        ]
      },
      {
        id: 2,
        description: 'Groceries',
        amount: 85.50,
        paidBy: 'Alex',
        date: new Date('2023-05-08'),
        participants: ['You', 'Alex'],
        splits: [
          { name: 'You', amount: 42.75 },
          { name: 'Alex', amount: 42.75 }
        ]
      },
      {
        id: 3,
        description: 'Utilities',
        amount: 150.00,
        paidBy: 'You',
        date: new Date('2023-05-05'),
        participants: ['You', 'Alex', 'Jamie'],
        splits: [
          { name: 'You', amount: 50.00 },
          { name: 'Alex', amount: 50.00 },
          { name: 'Jamie', amount: 50.00 }
        ]
      },
      {
        id: 4,
        description: 'Movie night snacks',
        amount: 35.00,
        paidBy: 'Jamie',
        date: new Date('2023-05-03'),
        participants: ['You', 'Alex', 'Jamie'],
        splits: [
          { name: 'You', amount: 11.67 },
          { name: 'Alex', amount: 11.67 },
          { name: 'Jamie', amount: 11.66 }
        ]
      }
    ];
    
    this.calculateBalances();
  }

  calculateBalances(): void {
    // Calculate who owes whom
    this.balances = [];
    
    // First, calculate net balances for each member
    const netBalances = new Map<string, number>();
    
    this.expenses.forEach(expense => {
      const payer = expense.paidBy;
      
      // Add the full amount to what the payer paid
      netBalances.set(payer, (netBalances.get(payer) || 0) + expense.amount);
      
      // Subtract each person's share from their balance
      expense.splits.forEach(split => {
        netBalances.set(split.name, (netBalances.get(split.name) || 0) - split.amount);
      });
    });
    
    // Convert to array of {name, balance}
    const balanceArray = Array.from(netBalances.entries()).map(([name, balance]) => ({
      name,
      balance
    }));
    
    // Sort by balance (negative first - these people owe money)
    balanceArray.sort((a, b) => a.balance - b.balance);
    
    // Calculate who pays whom
    const debtors = balanceArray.filter(item => item.balance < 0);
    const creditors = balanceArray.filter(item => item.balance > 0);
    
    let debtorIndex = 0;
    let creditorIndex = 0;
    
    while (debtorIndex < debtors.length && creditorIndex < creditors.length) {
      const debtor = debtors[debtorIndex];
      const creditor = creditors[creditorIndex];
      
      const amount = Math.min(Math.abs(debtor.balance), creditor.balance);
      
      if (amount > 0.01) { // Ignore very small amounts
        this.balances.push({
          from: debtor.name,
          to: creditor.name,
          amount
        });
      }
      
      // Update balances
      debtor.balance += amount;
      creditor.balance -= amount;
      
      // Move to next person if their balance is settled
      if (Math.abs(debtor.balance) < 0.01) {
        debtorIndex++;
      }
      
      if (Math.abs(creditor.balance) < 0.01) {
        creditorIndex++;
      }
    }
  }

  openExpenseModal(): void {
    this.isExpenseModalOpen = true;
  }

  closeExpenseModal(): void {
    this.isExpenseModalOpen = false;
  }

  saveExpense(expenseData: any): void {
    // In a real app, this would be an API call
    // For demo, just add to the local array
    const newExpense: Expense = {
      id: this.expenses.length + 1,
      ...expenseData
    };
    
    this.expenses.unshift(newExpense);
    this.calculateBalances();
  }
}