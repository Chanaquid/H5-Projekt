import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { UserService } from '../../services/user-service';
import { UserDTO } from '../../dtos/userDTO';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-admin-user',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './admin-user.html',
  styleUrl: './admin-user.css',
})

export class AdminUser implements OnInit {
 
  allUsers: UserDTO.AdminUserDTO[] = [];
  filteredUsers: UserDTO.AdminUserDTO[] = [];
  isLoading = true;
  searchQuery = '';
  activeTab: 'all' | 'verified' | 'unverified' | 'admin' | 'deleted' = 'all';
 
  // Modal
  showUserModal = false;
  isLoadingDetail = false;
  selectedUser: UserDTO.AdminUserDTO | null = null;
  userDetail: UserDTO.AdminUserDetailDTO | null = null;
 
  // Score adjust
  scoreAdjustment: number | null = null;
  scoreNote = '';
  isAdjustingScore = false;
  scoreError = '';
  scoreSuccess = '';
  visibleScoreHistory = 5;
 
  // Edit
  showEditForm = false;
  editForm: UserDTO.AdminEditUserDTO | null = null;
  isSavingEdit = false;
  editError = '';
  editSuccess = '';
 
  // Delete
  showDeleteConfirm = false;
  isDeletingUser = false;
  deleteWarnings: string[] = [];
 
  // Actions
  actionError = '';
  actionSuccess = '';
 
  tabs = [
    { key: 'all' as const,        label: 'All' },
    { key: 'verified' as const,   label: 'Verified' },
    { key: 'unverified' as const, label: 'Unverified' },
    { key: 'admin' as const,      label: 'Admins' },
    { key: 'deleted' as const,    label: 'Deleted' },
  ];
 
  constructor(
    private authService: AuthService,
    private userService: UserService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}
 
  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }
    this.loadUsers();
  }
 
  // ─── Load ────────────────────────────────────────────────────────────────
 
  private loadUsers(): void {
    this.isLoading = true;
    this.userService.getAllUsers().subscribe({
      next: (users) => {
        this.allUsers = users;
        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }
 
  // ─── Filters ─────────────────────────────────────────────────────────────
 
  applyFilters(): void {
    let result = [...this.allUsers];
 
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(u =>
        u.fullName.toLowerCase().includes(q) ||
        u.username.toLowerCase().includes(q) ||
        u.email.toLowerCase().includes(q)
      );
    }
 
    switch (this.activeTab) {
      case 'verified':   result = result.filter(u => u.isVerified && !u.isDeleted); break;
      case 'unverified': result = result.filter(u => !u.isVerified && !u.isDeleted); break;
      case 'admin':      result = result.filter(u => u.role === 'Admin' && !u.isDeleted); break;
      case 'deleted':    result = result.filter(u => u.isDeleted); break;
      default:           result = result.filter(u => !u.isDeleted); break;
    }
 
    this.filteredUsers = result;
    this.cdr.detectChanges();
  }
 
  getTabCount(key: string): number {
    switch (key) {
      case 'all':        return this.allUsers.filter(u => !u.isDeleted).length;
      case 'verified':   return this.allUsers.filter(u => u.isVerified && !u.isDeleted).length;
      case 'unverified': return this.allUsers.filter(u => !u.isVerified && !u.isDeleted).length;
      case 'admin':      return this.allUsers.filter(u => u.role === 'Admin' && !u.isDeleted).length;
      case 'deleted':    return this.allUsers.filter(u => u.isDeleted).length;
      default:           return 0;
    }
  }
 
  get verifiedCount()   { return this.allUsers.filter(u => u.isVerified && !u.isDeleted).length; }
  get unverifiedCount() { return this.allUsers.filter(u => !u.isVerified && !u.isDeleted).length; }
  get adminCount()      { return this.allUsers.filter(u => u.role === 'Admin' && !u.isDeleted).length; }
  get deletedCount()    { return this.allUsers.filter(u => u.isDeleted).length; }
 
  // ─── Modal ───────────────────────────────────────────────────────────────
 
  openUserModal(user: UserDTO.AdminUserDTO): void {
    this.selectedUser      = user;
    this.userDetail        = null;
    this.showUserModal     = true;
    this.isLoadingDetail   = true;
    this.showEditForm      = false;
    this.editForm          = null;
    this.scoreAdjustment   = null;
    this.scoreNote         = '';
    this.scoreError        = '';
    this.scoreSuccess      = '';
    this.actionError       = '';
    this.actionSuccess     = '';
    this.editError         = '';
    this.editSuccess       = '';
    this.visibleScoreHistory = 5;
 
    this.userService.getUserById(user.id).subscribe({
      next: (detail) => {
        this.userDetail      = detail;
        this.isLoadingDetail = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingDetail = false;
        this.showUserModal   = false;
        this.cdr.detectChanges();
      }
    });
  }
 
  // ─── Score adjustment ────────────────────────────────────────────────────
 
  adjustScore(): void {
    if (!this.userDetail || !this.scoreAdjustment) return;
    this.isAdjustingScore = true;
    this.scoreError = '';
 
    this.userService.adjustScore(this.userDetail.id, {
      pointsChanged: this.scoreAdjustment,
      note: this.scoreNote.trim() || ''
    }).subscribe({
      next: () => {
        const newScore = Math.min(100, Math.max(0, this.userDetail!.score + this.scoreAdjustment!));
        this.userDetail!.score = newScore;
        this.scoreAdjustment   = null;
        this.scoreNote         = '';
        this.isAdjustingScore  = false;
        this.scoreSuccess      = `Score adjusted to ${newScore}.`;
        const idx = this.allUsers.findIndex(u => u.id === this.userDetail!.id);
        if (idx !== -1) this.allUsers[idx].score = newScore;
        // Reload detail to get fresh score history
        this.userService.getUserById(this.userDetail!.id).subscribe({
          next: (d) => { this.userDetail = d; this.cdr.detectChanges(); }
        });
        this.cdr.detectChanges();
        setTimeout(() => { this.scoreSuccess = ''; this.cdr.detectChanges(); }, 3000);
      },
      error: (err) => {
        this.scoreError       = err.error?.message ?? 'Failed to adjust score.';
        this.isAdjustingScore = false;
        this.cdr.detectChanges();
      }
    });
  }
 
  // ─── Edit ────────────────────────────────────────────────────────────────
 
  openEdit(): void {
    if (!this.userDetail) return;
    this.editForm = {
      fullName:  this.userDetail.fullName,
      username:  this.userDetail.username,
      email:     this.userDetail.email,
      avatarUrl: this.userDetail.avatarUrl ?? '',
      gender:    this.userDetail.gender ?? '',
      address:   this.userDetail.address ?? '',
      role:      this.userDetail.role,
    };
    this.showEditForm = true;
  }
 
  saveEdit(): void {
    if (!this.userDetail || !this.editForm) return;
    this.isSavingEdit = true;
    this.editError    = '';
 
    this.userService.adminEditUser(this.userDetail.id, this.editForm).subscribe({
      next: (updated) => {
        this.userDetail   = { ...this.userDetail!, ...updated };
        this.isSavingEdit = false;
        this.editSuccess  = '✓ User updated.';
        this.showEditForm = false;
        const idx = this.allUsers.findIndex(u => u.id === updated.id);
        if (idx !== -1) this.allUsers[idx] = { ...this.allUsers[idx], ...updated };
        this.applyFilters();
        this.cdr.detectChanges();
        setTimeout(() => { this.editSuccess = ''; this.cdr.detectChanges(); }, 3000);
      },
      error: (err) => {
        this.editError    = err.error?.message ?? 'Failed to update user.';
        this.isSavingEdit = false;
        this.cdr.detectChanges();
      }
    });
  }
 
  // ─── Toggle verified ─────────────────────────────────────────────────────
 
  toggleVerified(): void {
    if (!this.userDetail) return;
    this.actionError = '';
    const newValue = !this.userDetail.isVerified;
 
    this.userService.adminEditUser(this.userDetail.id, { isVerified: newValue }).subscribe({
      next: (updated) => {
        this.userDetail!.isVerified = updated.isVerified;
        this.actionSuccess = updated.isVerified ? '✓ User verified.' : '✓ User unverified.';
        const idx = this.allUsers.findIndex(u => u.id === updated.id);
        if (idx !== -1) this.allUsers[idx].isVerified = updated.isVerified;
        this.applyFilters();
        this.cdr.detectChanges();
        setTimeout(() => { this.actionSuccess = ''; this.cdr.detectChanges(); }, 3000);
      },
      error: (err) => {
        this.actionError = err.error?.message ?? 'Failed to update verification.';
        this.cdr.detectChanges();
      }
    });
  }
 
  // ─── Delete ──────────────────────────────────────────────────────────────
 
  deleteUser(): void {
    if (!this.userDetail) return;
    this.isDeletingUser = true;
 
    this.userService.adminDeleteUser(this.userDetail.id).subscribe({
      next: (result: any) => {
        this.deleteWarnings    = result?.warnings ?? [];
        this.isDeletingUser    = false;
        this.showDeleteConfirm = false;
        this.showUserModal     = false;
        const idx = this.allUsers.findIndex(u => u.id === this.userDetail!.id);
        if (idx !== -1) this.allUsers[idx].isDeleted = true;
        this.applyFilters();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.actionError       = err.error?.message ?? 'Failed to delete user.';
        this.isDeletingUser    = false;
        this.showDeleteConfirm = false;
        this.cdr.detectChanges();
      }
    });
  }
 
  // ─── Helpers ─────────────────────────────────────────────────────────────
 
  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
 
  getScoreClass(score: number): string {
    if (score >= 70) return 'text-emerald-400';
    if (score >= 40) return 'text-amber-400';
    return 'text-red-400';
  }
 
  getBorrowingStatusClass(status: string): string {
    switch (status) {
      case 'Free':          return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'AdminApproval': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'Blocked':       return 'bg-red-400/10 text-red-400 border-red-400/20';
      default:              return 'bg-zinc-700 text-zinc-400 border-zinc-600';
    }
  }
 
  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active':       return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'approved':     return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'returned':     return 'bg-cyan-400/10 text-cyan-400 border-cyan-400/20';
      case 'late':         return 'bg-red-400/10 text-red-400 border-red-400/20';
      case 'pending':
      case 'adminpending': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'cancelled':
      case 'rejected':     return 'bg-zinc-700 text-zinc-400 border-zinc-600';
      default:             return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }
 
  getFineStatusClass(status: string): string {
    switch (status) {
      case 'Unpaid':              return 'text-red-400';
      case 'PendingVerification': return 'text-amber-400';
      case 'Paid':                return 'text-emerald-400';
      default:                    return 'text-zinc-400';
    }
  }
}