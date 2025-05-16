import { Component, type OnInit } from "@angular/core"
import { FormBuilder, type FormGroup, ReactiveFormsModule, Validators } from "@angular/forms"
import type { User } from "../../models/user.model"
import { UserService } from "../../services/user.service"
import { MatSnackBar } from "@angular/material/snack-bar"
import { MatCardModule } from "@angular/material/card"
import { MatIconModule } from "@angular/material/icon"
import { CommonModule } from "@angular/common"
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatError, MatFormField, MatLabel } from "@angular/material/form-field"
import { MatOption } from "@angular/material/core"
@Component({
  selector: "app-profile",
  templateUrl: "./profile.component.html",
  standalone: true,
  imports: [MatCardModule, MatIconModule, CommonModule, MatTabsModule, MatButtonModule, MatMenuModule, ReactiveFormsModule, MatFormField, MatLabel, MatError, MatOption],
  styleUrls: ["./profile.component.scss"],
})
export class ProfileComponent implements OnInit {
  profileForm: FormGroup
  user: User
  currencies = ["USD", "EUR", "GBP", "INR", "JPY", "CAD", "AUD"]

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private snackBar: MatSnackBar,
  ) {
    this.user = this.userService.getCurrentUser()

    this.profileForm = this.fb.group({
      name: [this.user.name, [Validators.required, Validators.minLength(2)]],
      email: [this.user.email, [Validators.required, Validators.email]],
      phone: [this.user.phone || ""],
      profilePic: [this.user.profilePic || ""],
      currency: ["INR", Validators.required],
      notifications: [true],
    })
  }

  ngOnInit(): void {}

  onSubmit(): void {
    if (this.profileForm.invalid) return

    const formValue = this.profileForm.value

    // Update user
    this.userService.updateUser({
      ...this.user,
      name: formValue.name,
      email: formValue.email,
      phone: formValue.phone || undefined,
      profilePic: formValue.profilePic || undefined,
    })

    this.snackBar.open("Profile updated successfully", "Close", {
      duration: 3000,
    })
  }
}
