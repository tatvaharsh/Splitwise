import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';

interface Expense {
  id: number;
  description: string;
  amount: number;
  paidBy: string;
  date: Date;
  group?: string;
  participants: string[];
}

interface Group {
  id: number;
  name: string;
  members: string[];
  totalExpenses: number;
}

interface Friend {
  id: number;
  name: string;
  balance: number;
  photoUrl: string;
}

@Component({
  selector: 'app-dashboard-component',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard-component.component.html',
  styleUrl: './dashboard-component.component.scss'
})
export class DashboardComponent implements OnInit {
  totalBalance: number = 0;
  youOwe: number = 0;
  youAreOwed: number = 0;
  recentExpenses: Expense[] = [];
  groups: Group[] = [];
  friends: Friend[] = [];
  isExpenseModalOpen: boolean = false;
  isMobileMenuOpen: boolean = false;

  currentDate: string;

  constructor() {
    this.currentDate = new Date().toISOString().substring(0, 10);
  }

  ngOnInit(): void {
    // In a real app, these would come from your API
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    // Mock data - would be replaced with API calls
    this.youOwe = 125.50;
    this.youAreOwed = 230.75;
    this.totalBalance = this.youAreOwed - this.youOwe;

    this.recentExpenses = [
      {
        id: 1,
        description: 'Dinner at Italian Restaurant',
        amount: 120.00,
        paidBy: 'You',
        date: new Date('2023-05-10'),
        group: 'Roommates',
        participants: ['You', 'Alex', 'Jamie']
      },
      {
        id: 2,
        description: 'Groceries',
        amount: 85.50,
        paidBy: 'Alex',
        date: new Date('2023-05-08'),
        group: 'Roommates',
        participants: ['You', 'Alex']
      },
      {
        id: 3,
        description: 'Movie tickets',
        amount: 45.00,
        paidBy: 'Jamie',
        date: new Date('2023-05-05'),
        participants: ['You', 'Jamie']
      }
    ];

    this.groups = [
      {
        id: 1,
        name: 'Roommates',
        members: ['You', 'Alex', 'Jamie'],
        totalExpenses: 450.75
      },
      {
        id: 2,
        name: 'Trip to New York',
        members: ['You', 'Chris', 'Taylor', 'Morgan'],
        totalExpenses: 1250.30
      },
      {
        id: 3,
        name: 'Couple Expenses',
        members: ['You', 'Sam'],
        totalExpenses: 325.45
      }
    ];

    this.friends = [
      {
        id: 1,
        name: 'Alex Johnson',
        balance: -45.50, // negative means you owe them
        photoUrl: 'assets/avatars/alex.jpg'
      },
      {
        id: 2,
        name: 'Jamie Smith',
        balance: 120.25, // positive means they owe you
        photoUrl: 'assets/avatars/jamie.jpg'
      },
      {
        id: 3,
        name: 'Taylor Brown',
        balance: 85.00,
        photoUrl: 'assets/avatars/taylor.jpg'
      }
    ];
  }

  openExpenseModal(): void {
    this.isExpenseModalOpen = true;
  }

  closeExpenseModal(): void {
    this.isExpenseModalOpen = false;
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }
}