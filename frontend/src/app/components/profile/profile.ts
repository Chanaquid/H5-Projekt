import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth-service';
import { ItemDTO } from '../../dtos/itemDTO';
import { UserDTO } from '../../dtos/userDTO';
import { LoanDTO } from '../../dtos/loanDTO';
import { UserService } from '../../services/user-service';
import { ItemService } from '../../services/item-service';
import { LoanService } from '../../services/loan-service';
import { FineDTO } from '../../dtos/fineDTO';
import { FineService } from '../../services/fine-service';

@Component({
  selector: 'app-profile',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile implements OnInit {

  // Data
  profile: UserDTO.UserProfileDTO | null = null;
  myItems: ItemDTO.ItemSummaryDTO[] = [];
  myLoans: LoanDTO.LoanSummaryDTO[] = [];
  scoreHistory: UserDTO.ScoreHistoryDTO[] = [];
  myFines: FineDTO.FineResponseDTO[] = [];
  showAvatarModal = false;

  // Stats
  stats: { icon: string; value: string | number; label: string; currency?: string }[] = [];

  // Tabs
  activeTab: 'items' | 'loans' | 'score' = 'items';
  tabs = [
    { key: 'items' as const, label: 'My Items' },
    { key: 'loans' as const, label: 'Loans' },
    { key: 'score' as const, label: 'Score History' },
  ];

  addressSuggestions: any[] = [];
  showAddressSuggestions = false;
  private addressSearchTimeout: any;

  //tracking
  private loadedFlags = { profile: false, items: false, loans: false, fines: false };


  // Edit profile
  editMode = false;
  isSaving = false;
  updateSuccess = false;
  updateError = '';
  editForm: UserDTO.UpdateProfileDTO = {
    fullName: '',
    userName: '',
    address: '',
    gender: '',
    avatarUrl: '',
    latitude: undefined,
    longitude: undefined,
  };

  // Change password
  passwordMode = false;
  isSavingPassword = false;
  passwordSuccess = false;
  passwordError = '';
  passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };

  // Delete account
  showDeleteConfirm = false;
  deletePassword = '';
  isDeletingAccount = false;
  deleteError = '';


  ownedLoans: LoanDTO.LoanSummaryDTO[] = [];
  loanView: 'borrowed' | 'lent' = 'borrowed';



  resetPasswordForm() {
    this.passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
    this.passwordError = '';
    this.passwordSuccess = false;
  }

  private emojiMap: Record<string, string> = {
    electronics: '📱', tools: '🔧', sports: '⚽', music: '🎸',
    books: '📚', camping: '⛺', photography: '📷', gaming: '🎮',
    gardening: '🌱', biking: '🚲', kitchen: '🍳', cleaning: '🧹',
    fashion: '👗', art: '🎨', baby: '👶', events: '🎉', auto: '🚗', other: '📦',
  };

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private itemService: ItemService,
    private loanService: LoanService,
    private fineService: FineService,
    private router: Router,
    private http: HttpClient,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit() {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    this.loadProfile();
    this.loadItems();
    this.loadLoans();
    this.loadScoreHistory();
    this.loadFines();
    this.loadOwnedLoans();

  }

  openAvatarModal(): void {
    this.showAvatarModal = true;
  }

  private loadProfile() {
    this.userService.getMe().subscribe({
      next: (p) => {
        this.profile = p;
        this.editForm = {
          fullName: p.fullName,
          userName: p.username,
          address: p.address,
          gender: p.gender ?? '',
          avatarUrl: p.avatarUrl ?? '',
          latitude: p.latitude,
          longitude: p.longitude,
        };
        this.loadedFlags.profile = true;
        this.checkAndBuildStats();
        this.cdr.detectChanges();
      },
    });
  }

  private loadItems() {
    this.itemService.getMyItems().subscribe({
      next: (items) => {
        this.myItems = items;
        this.loadedFlags.items = true;
        this.checkAndBuildStats();
        this.cdr.detectChanges();
      },
    });
  }

  private loadLoans() {
    this.loanService.getBorrowedLoans().subscribe({
      next: (loans) => {
        this.myLoans = loans;
        this.loadedFlags.loans = true;
        this.checkAndBuildStats();
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadedFlags.loans = true; //still mark done so stats aren't blocked
        this.checkAndBuildStats();
      },
    });
  }

  private loadScoreHistory() {
    this.userService.getScoreHistory().subscribe({
      next: (history) => {
        this.scoreHistory = history;
        this.cdr.detectChanges();
      },
    });
  }

  private loadFines() {
    this.fineService.getMyFines().subscribe({
      next: (fines) => {
        this.myFines = fines;
        this.loadedFlags.fines = true;
        this.checkAndBuildStats();
        this.cdr.detectChanges();
        console.log(fines)
      },
      error: () => {
        this.loadedFlags.fines = true;
        this.checkAndBuildStats();
      },
    });
  }

  private loadOwnedLoans() {
  this.loanService.getOwnedLoans().subscribe({
    next: (loans) => {
      this.ownedLoans = loans;
      this.cdr.detectChanges();
    },
    error: () => {}
  });
}

  private checkAndBuildStats() {
    const { profile, items, loans, fines } = this.loadedFlags;
    if (profile && items && loans && fines) {
      this.buildStats();
      this.cdr.detectChanges();
    }
  }

  private buildStats() {
    const activeItems = this.myItems.filter(i => i.status === 'Approved').length;
    const activeLoans = this.myLoans.filter(l => l.status === 'Active' || l.status === 'Approved').length;
    const completedLoans = this.myLoans.filter(l => l.status === 'Returned').length;
    const totalFinesPaid = this.myFines
      .filter(f => f.status === 'Paid')
      .reduce((sum, f) => sum + f.amount, 0);

    this.stats = [
      { icon: '📦', value: activeItems, label: 'Active items' },
      { icon: '🤝', value: activeLoans, label: 'Active loans' },
      { icon: '✅', value: completedLoans, label: 'Completed loans' },
      { icon: '💸', value: totalFinesPaid, currency: 'kr', label: 'Total fines paid' }
    ];
  }

  goToLoan(id: number): void {
    this.router.navigate(['/loans', id]);
  }

  saveProfile() {
    this.isSaving = true;
    this.updateSuccess = false;
    this.updateError = '';

    this.userService.updateProfile(this.editForm).subscribe({
      next: (updated) => {
        this.profile = updated;
        this.isSaving = false;
        this.updateSuccess = true;
        this.editMode = false;
        this.cdr.detectChanges();
        setTimeout(() => { this.updateSuccess = false; this.cdr.detectChanges(); }, 3000);
      },
      error: (err) => {
        this.updateError = err.error?.message ?? 'Failed to update profile.';
        this.isSaving = false;
        this.cdr.detectChanges();
      },
    });
  }

  changePassword() {
    if (!this.passwordForm.currentPassword || !this.passwordForm.newPassword) {
      this.passwordError = 'All fields are required.';
      return;
    }
    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      this.passwordError = 'Passwords do not match.';
      return;
    }
    if (this.passwordForm.newPassword.length < 6) {
      this.passwordError = 'Password must be at least 6 characters.';
      return;
    }

    this.isSavingPassword = true;
    this.passwordSuccess = false;
    this.passwordError = '';

    this.authService.changePassword(this.passwordForm.currentPassword, this.passwordForm.newPassword)
      .subscribe({
        next: () => {
          this.isSavingPassword = false;
          this.passwordSuccess = true;
          this.resetPasswordForm();
          this.passwordMode = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.passwordError = err.error?.message ?? 'Failed to update password.';
          this.isSavingPassword = false;
          this.cdr.detectChanges();
        },
      });
  }

  deleteAccount() {
    if (!this.deletePassword) return;
    this.isDeletingAccount = true;
    this.deleteError = '';

    this.userService.deleteAccount({ password: this.deletePassword }).subscribe({
      next: () => {
        this.authService.clearTokens();
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.deleteError = err.error?.message ?? 'Failed to delete account.';
        this.isDeletingAccount = false;
        this.cdr.detectChanges();
      },
    });
  }

  onAddressInput(value: string) {
    clearTimeout(this.addressSearchTimeout);
    this.showAddressSuggestions = false;

    if (!value || value.length < 3) {
      this.addressSuggestions = [];
      return;
    }

    this.addressSearchTimeout = setTimeout(() => {
      fetch(`https://nominatim.openstreetmap.org/search?format=json&addressdetails=1&q=${encodeURIComponent(value)}&limit=5`)
        .then(res => res.json())
        .then(data => {
          this.addressSuggestions = data;
          this.showAddressSuggestions = true;
          this.cdr.detectChanges();
        });
    }, 400);
  }

  selectAddress(place: any) {
    const a = place.address;
    const road = a?.road ?? '';
    const houseNumber = a?.house_number ?? '';
    const neighbourhood = a?.neighbourhood ?? a?.suburb ?? '';
    const city = a?.city ?? a?.town ?? a?.village ?? '';

    const parts = [
      [road, houseNumber].filter(Boolean).join(' '),
      neighbourhood,
      city,
    ].filter(Boolean);

    this.editForm.address = parts.join(', ') || place.display_name;
    this.editForm.latitude = parseFloat(place.lat);
    this.editForm.longitude = parseFloat(place.lon);
    this.addressSuggestions = [];
    this.showAddressSuggestions = false;
    this.cdr.detectChanges();
  }

  goToItem(id: number) {
    this.router.navigate(['/items', id]);
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getCategoryEmoji(cat: string): string {
    return this.emojiMap[cat.toLowerCase()] ?? '📦';
  }

  getLoanStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'active': return 'bg-emerald-400/10 text-emerald-400';
      case 'approved': return 'bg-blue-400/10 text-blue-400';
      case 'returned': return 'bg-zinc-300/10 text-zinc-200';
      case 'overdue': return 'bg-red-400/10 text-red-400';
      case 'pending': return 'bg-amber-400/10 text-amber-400';
      default: return 'bg-zinc-700 text-zinc-400';
    }
  }
}