import { Component, type OnInit } from "@angular/core"
import type { Observable } from "rxjs"
import type { Group } from "../../models/group.model"
import { AddGroupComponent } from "../add-group/add-group.component"
import { GroupService } from "../../services/group.service"
import { Router } from "@angular/router"
import { MatDialog } from "@angular/material/dialog"
import { MatCard } from "@angular/material/card"
import { MatIcon } from "@angular/material/icon"
import { CommonModule } from "@angular/common"
import { HttpClientModule } from "@angular/common/http"
import { IResponse } from "../../generic/response"

@Component({
  selector: "app-groups",
  templateUrl: "./groups.component.html",
  imports: [MatCard, MatIcon,CommonModule,HttpClientModule],
  standalone: true,
  styleUrls: ["./groups.component.scss"],
})
export class GroupsComponent implements OnInit {
  groups$: Group[] = [];
  constructor(
    private groupService: GroupService,
    private router: Router,
    private dialog: MatDialog,
  ) {}  

  ngOnInit(): void {
    this.groupService.getGroups().subscribe((res) => {
        this.groups$ = res.content;
      });
  }

  openGroup(groupId: string): void {
    this.router.navigate(["/groups", groupId])
  }

  openAddGroupDialog(): void {
    this.dialog.open(AddGroupComponent, {
      width: "500px",
      maxWidth: "95vw",
    })
  }

  getGroupMembersText(group: Group): string {
    return `${group.totalMember} ${group.totalMember === 1 ? "friend" : "friends"}`
  }
}
