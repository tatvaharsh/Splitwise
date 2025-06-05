import { Component, type OnInit } from "@angular/core"
import { FormBuilder, type FormGroup, ReactiveFormsModule, Validators } from "@angular/forms"
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog"
import { FriendService } from "../../services/friend.service"
import { ActivityService } from "../../services/activity.service"
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core'; // For native date adapter
import { MatIconModule } from '@angular/material/icon'; // For matSuffix icon toggle
import { CommonModule } from "@angular/common"

@Component({
  selector: "app-add-friend",
  templateUrl: "./add-friend.component.html",
  standalone: true,
  imports: [MatFormFieldModule, MatSelectModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatIconModule, CommonModule,ReactiveFormsModule],
  styleUrls: ["./add-friend.component.scss"],
})
export class AddFriendComponent implements OnInit {
  friendForm: FormGroup

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<AddFriendComponent>,
    private friendService: FriendService,
    private activityService: ActivityService,
  ) {
    this.friendForm = this.fb.group({
      name: ["", [Validators.required, Validators.minLength(2)]],
      email: ["", [Validators.required, Validators.email]]
    })
  }

  ngOnInit(): void {}

  onSubmit(): void {
    if (this.friendForm.valid) {
      this.friendService.addFriend(this.friendForm.value).subscribe({
        next: () => {
          this.dialogRef.close(); 
          window.location.reload(); 
        }
      });
    }
    this.dialogRef.close()
  }
  

  onCancel(): void {
    this.dialogRef.close()
  }
}
