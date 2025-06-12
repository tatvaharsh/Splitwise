import { Component } from "@angular/core"
import { AddExpenseComponent } from "./components/add-expense/add-expense.component"
import { AddGroupComponent } from "./components/add-group/add-group.component"
import { AddFriendComponent } from "./components/add-friend/add-friend.component"
import { MatDialog } from "@angular/material/dialog"
import { Router } from "@angular/router"
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { RouterOutlet } from "@angular/router"
@Component({
  selector: "app-root",
  standalone: true,
  imports: [    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatDividerModule, 
    RouterOutlet,
    ],
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.scss"],
})
export class AppComponent {
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
}