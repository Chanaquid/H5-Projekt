import { ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  email = '';
  password = '';
  errorMessage = '';
  isLoading = false;
  showPassword = false;

  constructor(private authService: AuthService, private router: Router, private cdr: ChangeDetectorRef) {}

  login() {
    this.isLoading = true;
  this.errorMessage = '';
  const dto = { email: this.email, password: this.password };

  this.authService.login(dto).subscribe({
    next: (res) => {
      this.isLoading = false;
      console.log('Login successful:', res);
      this.router.navigate(['/home']);
    },
    error: (err) => {
      this.isLoading = false;
      console.log(err)
      this.errorMessage = err.error?.message || 'Invalid email or password';
      this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW
    }
  });
  }

  togglePassword() {
      this.showPassword = !this.showPassword;
  }
  
}
