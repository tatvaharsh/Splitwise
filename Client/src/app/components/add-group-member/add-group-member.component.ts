import { Component, type OnInit, Inject } from "@angular/core"
import { FormBuilder, type FormGroup, ReactiveFormsModule, Validators } from "@angular/forms"
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog"
import type { User } from "../../models/user.model"
import { ActivityService } from "../../services/activity.service"
import { GroupService } from "../../services/group.service"
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core'; // For native date adapter
import { MatIconModule } from '@angular/material/icon'; // For matSuffix icon toggle
import { CommonModule } from "@angular/common"

@Component({
  selector: "app-add-group-member",
  templateUrl: "./add-group-member.component.html",
  standalone: true,
  imports: [MatFormFieldModule, MatSelectModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatIconModule, CommonModule, ReactiveFormsModule],
  styleUrls: ["./add-group-member.component.scss"],
})
export class AddGroupMemberComponent implements OnInit {
  memberForm: FormGroup
  availableUsers: User[] = []
  groupId: string
  data: any

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<AddGroupMemberComponent>,
    @Inject(MAT_DIALOG_DATA) data: any,
    private groupService: GroupService,
    private activityService: ActivityService,
  ) {
    this.groupId = data.groupId
    this.data = data

    this.memberForm = this.fb.group({
      userId: ["", Validators.required],
    })

    // Get available users (not already in the group)
    // this.availableUsers = this.groupService.getAvailableUsers(this.groupId)
  }

  ngOnInit(): void {}

  onSubmit(): void {
    if (this.memberForm.invalid) return

    const formValue = this.memberForm.value
    const selectedUser = this.availableUsers.find((u) => u.id === formValue.userId)

    if (selectedUser) {
      // Add member to group
    //   this.groupService.addMemberToGroup(this.groupId, selectedUser)

      // Add activity
      this.activityService.addActivity({
        type: "group",
        description: `You added ${selectedUser.name} to the group`,
        date: new Date(),
        groupId: this.groupId,
        icon: "person_add",
      })
    }

    this.dialogRef.close()
  }

  onCancel(): void {
    this.dialogRef.close()
  }
}
