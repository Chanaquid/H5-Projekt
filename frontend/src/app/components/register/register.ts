import { ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthDTO } from '../../dtos/authDTO';
import { AuthService } from '../../services/auth-service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {

  dto: AuthDTO.RegisterDTO = {
    fullName: '',
    email: '',
    username: '',
    password: '',
    address: '',
    dateOfBirth: '',
    gender: '',
    avatarUrl: undefined,
    latitude: undefined,
    longitude: undefined,
  };

  isLoading = false;
  errorMessage = '';
  successMessage = '';
  suggestions: any[] = [];
  showSuggestions = false;
  private searchTimeout: any;

  constructor(
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  onAddressInput(value: string) {
    clearTimeout(this.searchTimeout);
    this.showSuggestions = false;

    if (value.length < 3) {
      this.suggestions = [];
      return;
    }

    this.searchTimeout = setTimeout(() => {
      fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(value)}&limit=5`)
        .then(res => res.json())
        .then(data => {
          this.suggestions = data;
          this.showSuggestions = true;
          this.cdr.detectChanges();
        });
    }, 400);
  }

  selectSuggestion(place: any) {
    this.dto.address = place.display_name;
    this.dto.latitude = parseFloat(place.lat);
    this.dto.longitude = parseFloat(place.lon);
    this.suggestions = [];
    this.showSuggestions = false;


  console.log('Selected address:', this.dto.address);
  console.log('Latitude:', this.dto.latitude);
  console.log('Longitude:', this.dto.longitude);
  }

  register() {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.register(this.dto).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Account created! Please check your email to confirm your account.';
        //Route
        this.router.navigate(["/login"]);

        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Registration failed. Please try again.';
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }
}