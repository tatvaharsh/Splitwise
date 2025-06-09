import { Component } from '@angular/core';
import { MatDialog } from "@angular/material/dialog"
import { Router } from "@angular/router"
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { RouterOutlet } from "@angular/router"
import { AddGroupComponent } from '../../components/add-group/add-group.component';
import { AddExpenseComponent } from '../../components/add-expense/add-expense.component';
import { AddFriendComponent } from '../../components/add-friend/add-friend.component';
@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatDividerModule, RouterOutlet],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent {
  title = "splitwise-clone"
  currentTab = "groups"

  constructor(
    private dialog: MatDialog,
    private router: Router,
  ) {}

  navigateTo(tab: string): void {
    this.currentTab = tab
    this.router.navigate([`/${tab}`])
  }

  openAddExpenseDialog(): void {
    this.dialog.open(AddExpenseComponent, {
      width: "500px",
      maxWidth: "95vw",
      panelClass: "expense-dialog",
    })
  }

  openAddGroupDialog(): void {
    if (this.currentTab === "groups") {
      this.dialog.open(AddGroupComponent, {
        width: "500px",
        maxWidth: "95vw",
      })
    }
  }

  openAddFriendDialog(): void {
    if (this.currentTab === "friends") {
      this.dialog.open(AddFriendComponent, {
        width: "500px",
        maxWidth: "95vw",
      })
    }
  }

  openProfileMenu(): void {
    this.router.navigate(["/profile"])
  }

  logout(): void {
    localStorage.clear();
    this.router.navigate(['/login']);
  }
}
