import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { IResponse } from '../../generic/response';
import { ILoginResponse } from '../../models/auth.model';
import { GlobalConstant } from '../../generic/global-const';
import { LocalStorageService } from '../../services/storage.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, CommonModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  loginForm: FormGroup;
  isLoading = false;
  showPassword = false;

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private authService: AuthService, 
    private store: LocalStorageService ,
    private toastr: ToastrService
  ) {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  onSubmit() {
    if (this.loginForm.valid) {
      this.isLoading = true;
  
      const loginPayload = {
        email: this.loginForm.get('email')?.value,
        password: this.loginForm.get('password')?.value,
      };
  
      this.authService.login(loginPayload).subscribe({
        next: (res: IResponse<ILoginResponse>) => {
          this.store.set(GlobalConstant.ACCESS_TOKEN, res.content);
          // Navigate based on role, default to '/groups'
          this.toastr.success('Data saved successfully!', 'Success');
          this.router.navigate(['/groups']);
          this.isLoading = false;
        },
        error: (error) => {
          this.isLoading = false;
          // Optionally, handle the error (e.g., show a notification)
          console.error('Login failed', error);
        }
      });
    } else {
      this.markFormGroupTouched();
    }
  }
  

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  navigateToSignup() {
    this.router.navigate(['/signup']);
  }

  private markFormGroupTouched() {
    Object.keys(this.loginForm.controls).forEach(key => {
      const control = this.loginForm.get(key);
      control?.markAsTouched();
    });
  }

  // Getters for easy access in template
  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }
}