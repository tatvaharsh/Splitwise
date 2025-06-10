import { Component, inject, Input, OnDestroy, OnInit } from '@angular/core';
import {
  FriendSettlementTransparency,
  UserBalanceDetail,
} from '../../models/settlement-transparency';
import { Subscription } from 'rxjs';
import { ActivatedRoute, Route, Router, RouterModule } from '@angular/router';
import { SettlementService } from '../../services/settlement.service ';
import { CommonModule } from '@angular/common';
import { IJwtPayload } from '../../models/auth.model';
import { LocalStorageService } from '../../services/storage.service';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-settlement-transparency',
  standalone: true,
  imports: [CommonModule, MatIcon, RouterModule],
  templateUrl: './settlement-transparency.component.html',
  styleUrl: './settlement-transparency.component.css',
})
export class SettlementTransparencyComponent implements OnInit, OnDestroy {
  @Input() friend2Id!: string; // Input property if passed from parent, or get from route
  settlementData: FriendSettlementTransparency | null = null;
  isLoading = true;
  error: string | null = null;
  showRawData = false;
  yourUserId! : string;
  yourUserName!: string;

  private routeSub!: Subscription;
  private dataSub!: Subscription;
  private router = inject(Router);
  constructor(
    private settlementService: SettlementService,
    private route: ActivatedRoute,
    
    private storageService: LocalStorageService
  ) {}

  ngOnInit(): void {
    const tokenPayload: IJwtPayload | null = this.storageService.getDecodedToken();
    this.yourUserId = tokenPayload?.UserId || ''
    this.yourUserName = tokenPayload?.Name || ''
    // Get friend2Id from route parameters if not provided as input
    this.routeSub = this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.friend2Id = id;
        this.loadSettlementData();
      } else {
        this.error = 'Friend ID not provided in route.';
        this.isLoading = false;
      }
    });
  }

  ngOnDestroy(): void {
    if (this.routeSub) {
      this.routeSub.unsubscribe();
    }
    if (this.dataSub) {
      this.dataSub.unsubscribe();
    }
  }

  loadSettlementData(): void {
    this.isLoading = true;
    this.error = null;
    this.dataSub = this.settlementService
      .getFriendSettlementTransparency(this.friend2Id)
      .subscribe({
        next: (response) => {
          if (response.success && response.content) {
            this.settlementData = response.content;
            // Optionally, try to get friend's name from data
            const friendBalance =
              this.settlementData.initialOneToOneActivityBalances.find(
                (b) => b.userId === this.friend2Id
              );
            if (friendBalance && friendBalance.userName) {
              // We can rely on the backend to provide the name if it's there
              // Otherwise, we might need a separate call to a user service for friend details
            }
          } else {
            this.error = response.message || 'Failed to load settlement data.';
          }
          this.isLoading = false;
        },
        error: (err) => {
          console.error('API Error:', err);
          this.error =
            'An error occurred while fetching data. Please try again.';
          this.isLoading = false;
        },
      });
  }

  getFriendName(): string {
    if (this.settlementData) {
      const friendData =
        this.settlementData.initialOneToOneActivityBalances.find(
          (b) => b.userId === this.friend2Id
        );
      if (friendData && friendData.userName) {
        return friendData.userName;
      }
    }
    // Fallback if name is not available or not yet loaded
    return 'Your Friend';
  }

  getUserBalance(balances: UserBalanceDetail[], userId: string): number {
    return balances.find((b) => b.userId === userId)?.balance || 0;
  }

  // Helper to format currency for display
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
    }).format(amount);
  }

  // Helper to get group name from initialGroupBalances
  getGroupName(groupId: string): string {
    const group = this.settlementData?.initialGroupBalances.find(
      (g) => g.groupId === groupId
    );
    return group?.groupName || 'Unknown Group';
  }

  // Helper to calculate overall net settlement text
  calculateOverallSettlementText(): string {
    if (!this.settlementData) return '';

    let netAmount = 0;

    // Sum one-to-one settlements
    this.settlementData.calculatedOneToOneSettlements.forEach((s) => {
      if (s.payerId === this.yourUserId && s.receiverId === this.friend2Id) {
        netAmount -= s.amount; // You pay friend
      } else if (
        s.payerId === this.friend2Id &&
        s.receiverId === this.yourUserId
      ) {
        netAmount += s.amount; // Friend pays you
      }
    });

    // Sum group settlements (only payments between these two friends)
    this.settlementData.calculatedGroupSettlements.forEach((s) => {
      if (s.payerId === this.yourUserId && s.receiverId === this.friend2Id) {
        netAmount -= s.amount; // You pay friend
      } else if (
        s.payerId === this.friend2Id &&
        s.receiverId === this.yourUserId
      ) {
        netAmount += s.amount; // Friend pays you
      }
    });

    if (netAmount > 0) {
      return `${this.getFriendName()} owes you ${this.formatCurrency(
        netAmount
      )}.`;
    } else if (netAmount < 0) {
      return `You owe ${this.getFriendName()} ${this.formatCurrency(
        Math.abs(netAmount)
      )}.`;
    } else {
      return `You are settled up with ${this.getFriendName()}!`;
    }
  }

  toggleRawData(): void {
    this.showRawData = !this.showRawData;
  }

  goBack(){
    this.router.navigate(['/friends', this.friend2Id]);
  }

}

