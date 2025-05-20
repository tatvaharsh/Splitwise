import { Component, Inject, OnInit } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { GroupService } from "../../services/group.service";
import { UserService } from "../../services/user.service";
import { ActivityService } from "../../services/activity.service";
import { CommonModule } from "@angular/common";
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { ReactiveFormsModule } from "@angular/forms";
import { HttpClientModule } from "@angular/common/http";

@Component({
  selector: "app-add-group",
  templateUrl: "./add-group.component.html",
  standalone: true,
  imports: [
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    HttpClientModule
  ],
  styleUrls: ["./add-group.component.scss"],
})
export class AddGroupComponent implements OnInit {
  groupForm: FormGroup;
  imagePreview: string | ArrayBuffer | null = null;
  selectedFile: File | null = null;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<AddGroupComponent>,
    private groupService: GroupService,
    private userService: UserService,
    private activityService: ActivityService,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.groupForm = this.fb.group({
      name: ["", [Validators.required, Validators.minLength(2)]],
      image: [""],
    });
  }
  mode: 'add' | 'edit' = 'add';


  ngOnInit(): void {
    if (this.data?.id) {
      this.mode = 'edit';
      this.groupForm.patchValue({
        name: this.data.groupname
      });
      this.imagePreview = this.data.autoLogo || null;
    }
  }
  
  

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.selectedFile = file;
      const reader = new FileReader();
      reader.onload = () => {
        this.imagePreview = reader.result;
      };
      reader.readAsDataURL(file);
    }
  }

  onSubmit(): void {
    const formData = new FormData();

    if (this.groupForm.valid){
      if (this.data?.id) {
        formData.append("Id", this.data.id); 
        formData.append("GroupName", this.groupForm.get("name")?.value);
        if (this.selectedFile) {
          formData.append("AutoLogo", this.selectedFile);
        }
        this.groupService.updateGroup(this.data.id, formData).subscribe({
          next: () => this.dialogRef.close()
        });
      }else{
        formData.append("GroupName", this.groupForm.get("name")?.value);
        if (this.selectedFile) {
          formData.append("AutoLogo", this.selectedFile);
        }
        this.groupService.createGroup(formData).subscribe({
          next: (response) => {
            this.dialogRef.close();
          },
          error: (error) => {
            console.error("Error creating group:", error);
          }
        });
      }
    }
    return;
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
