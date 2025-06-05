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
import { Member } from "../../models/expense.model"
import { Friend } from "../../models/friend.model"
import { FriendService } from "../../services/friend.service"

@Component({
  selector: "app-add-group-member",
  templateUrl: "./add-group-member.component.html",
  standalone: true,
  imports: [MatFormFieldModule, MatSelectModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatIconModule, CommonModule, ReactiveFormsModule],
  styleUrls: ["./add-group-member.component.scss"],
})
export class AddGroupMemberComponent implements OnInit {
  memberForm: FormGroup
  availableUsers: Member[] = []
  groupId: string
  data: any

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<AddGroupMemberComponent>,
    @Inject(MAT_DIALOG_DATA) data: any,
    private groupService: GroupService,
    private friendService: FriendService,

    private activityService: ActivityService,
  ) {
    this.groupId = data.groupId
    this.data = data

    this.memberForm = this.fb.group({
      userId: ["", Validators.required],
    })
  }

  ngOnInit(): void {
    this.friendService.getAvailableUsers(this.groupId).subscribe({
      next: (response) => {
        this.availableUsers = response.content;
      },
      error: (err) => {
        console.error('Failed to load users', err);
      }
    });
  }

  onSubmit(): void {
    if (this.memberForm.valid){
      const formValue = this.memberForm.value;
      const selectedUser = this.availableUsers.find((u) => u.id === formValue.userId);
    
      if (selectedUser) {
        this.friendService.addMemberToGroup(selectedUser.id, this.groupId).subscribe({
          next: () => {
            this.dialogRef.close();
            window.location.reload();
          }
        });
      }
    }
    return;
  }

  onCancel(): void {
    this.dialogRef.close()
  }
}
