import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth-service';
import { AdminService } from '../../services/admin-service';
import { AdminDTO } from '../../dtos/adminDTO';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-admin-dashboard',
  imports: [CommonModule, RouterLink, Navbar],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard implements OnInit {

  dashboard: AdminDTO.AdminDashboardDTO | null = null;
  isLoading = true;

  constructor(
    private authService: AuthService,
    private adminService: AdminService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn() || !this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.adminService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard = data;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get allClear(): boolean {
    if (!this.dashboard) return false;
    return (
      this.dashboard.pendingItemApprovals === 0 &&
      this.dashboard.pendingLoanApprovals === 0 &&
      this.dashboard.openDisputes === 0 &&
      this.dashboard.pendingAppeals === 0 &&
      this.dashboard.pendingVerifications === 0
    );
  }

  navigate(path: string): void {
    this.router.navigate([path]);
  }
}