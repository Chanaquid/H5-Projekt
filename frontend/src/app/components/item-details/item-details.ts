import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { ItemService } from '../../services/item-service';
import { ReviewService } from '../../services/review-service';
import { UserService } from '../../services/user-service';
import { ItemDTO } from '../../dtos/itemDTO';
import { ReviewDTO } from '../../dtos/reviewDTO';
import { Navbar } from '../navbar/navbar';
import { LoanService } from '../../services/loan-service';
import { LoanDTO } from '../../dtos/loanDTO';
import { DisputeService } from '../../services/dispute-service';

@Component({
  selector: 'app-item-details',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './item-details.html',
})
export class ItemDetails implements OnInit {

  item: ItemDTO.ItemDetailDTO | null = null;
  reviews: ReviewDTO.ItemReviewResponseDTO[] = [];
  isLoading = true;
  isLoadingReviews = true;
  selectedPhoto: string | null = null;
  currentUserId = '';
  isAdmin = false;

  // Loan request
  showLoanModal = false;
  loanForm = { startDate: '', endDate: '' };
  isRequesting = false;
  loanError = '';
  loanSuccess = '';
  existingLoan: LoanDTO.LoanDetailDTO | null = null;
  isCancellingLoan = false;
  cancelError = '';

  //Loan history
  loanHistory: LoanDTO.LoanSummaryDTO[] = [];
  isLoadingHistory = false;
  historyCollapsed = false;
  visibleLoans = 5;

  //dispute history
  disputeHistory: any[] = [];
  isLoadingDisputes = false;
  disputesCollapsed = false;
  visibleDisputes = 5;

  // Edit modal
  showEditModal = false;
  editForm = {
    title: '',
    description: '',
    condition: 'Good' as unknown as ItemDTO.UpdateItemDTO['condition'],
    currentValue: 0,
    availableFrom: '',
    availableUntil: '',
    minLoanDays: undefined as number | undefined,
    pickupAddress: '',
    pickupLatitude: 0 as number,
    pickupLongitude: 0 as number,
    requiresVerification: false,
    isActive: true,
    photoUrl: '',
  };
  isSavingEdit = false;
  editError = '';
  editSuccess = '';
  descriptionExpanded = false;

  // Address autocomplete
  addressSuggestions: any[] = [];
  showAddressSuggestions = false;
  private addressSearchTimeout: any;

  // Reviews
  visibleReviews = 5;
  get displayedReviews() { return this.reviews.slice(0, this.visibleReviews); }
  loadMoreReviews() { this.visibleReviews += 5; }

  get averageRating(): number {
    if (!this.reviews.length) return 0;
    return Math.round((this.reviews.reduce((s, r) => s + r.rating, 0) / this.reviews.length) * 10) / 10;
  }

  get isOwner(): boolean {
    return this.item?.owner?.id === this.currentUserId;
  }

  get canRequestLoan(): boolean {
    if (!this.item) return false;
    return this.item.status === 'Approved' &&
      this.item.isActive &&
      !this.item.isCurrentlyOnLoan &&
      !this.isOwner &&
      !this.existingLoan;
  }

  private emojiMap: Record<string, string> = {
    electronics: '📱', tools: '🔧', sports: '⚽', music: '🎸',
    books: '📚', camping: '⛺', photography: '📷', gaming: '🎮',
    gardening: '🌱', biking: '🚲', kitchen: '🍳', cleaning: '🧹',
    fashion: '👗', art: '🎨', baby: '👶', events: '🎉', auto: '🚗', other: '📦',
  };

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private authService: AuthService,
    private itemService: ItemService,
    private reviewService: ReviewService,
    private userService: UserService,
    private loanService: LoanService,
    private disputeService: DisputeService,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();

    if (this.authService.isLoggedIn()) {
      this.userService.getMe().subscribe({
        next: (u) => { this.currentUserId = u.id; this.cdr.detectChanges(); },
        error: () => { }
      });
    }

    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loadItem(id);

  }

  private loadItem(id: number): void {
    this.isLoading = true;
    this.itemService.getById(id).subscribe({
      next: (item) => {
        this.item = item;
        this.isLoading = false;
        console.log("Item", item)
        this.cdr.detectChanges();
        this.loadReviews(id);
        if (this.authService.isLoggedIn() && !this.isOwner) {
          this.checkExistingLoan();
        }

      // Only load history for owner or admin
        if (this.isOwner || this.isAdmin) {
        this.loadLoanHistory(id);
        this.loadDisputeHistory(id);
      }
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });


  }

  private checkExistingLoan(): void {
    if (!this.item) return;
    this.loanService.getBorrowedLoans().subscribe({
      next: (loans) => {
        const loan = loans.find(l =>
          l.itemTitle === this.item!.title &&
          ['Pending', 'AdminPending', 'Approved', 'Active', 'Late'].includes(l.status)
        );
        if (loan) {
          this.loanService.getById(loan.id).subscribe({
            next: (detail) => {
              if (detail.item.id === this.item!.id) {
                this.existingLoan = detail;
                this.cdr.detectChanges();
              }
            }
          });
        }
        this.cdr.detectChanges();
      },
      error: () => { }
    });
  }

  private loadReviews(itemId: number): void {
    this.reviewService.getItemReviews(itemId).subscribe({
      next: (reviews) => {
        this.reviews = reviews;
        this.isLoadingReviews = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingReviews = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadLoanHistory(itemId: number): void {
    this.isLoadingHistory = true;
    this.loanService.getLoansByItemId(itemId).subscribe({
      next: (loans) => {
        this.loanHistory = loans;
        this.isLoadingHistory = false;
        console.log('Loan History Loaded:', this.loanHistory);
        this.cdr.detectChanges();
      },
      error: () => { this.isLoadingHistory = false; }
    });
  }

  private loadDisputeHistory(itemId: number): void {
    this.isLoadingDisputes = true;
    this.disputeService.getDisputeHistoryByItemId(itemId).subscribe({
      next: (disputes) => {
        this.disputeHistory = disputes;
        this.isLoadingDisputes = false;
        console.log('Dispute History Loaded:', this.disputeHistory);
        this.cdr.detectChanges();
      },
      error: () => { this.isLoadingDisputes = false; }
    });
  }

  // ── Edit modal ──────────────────────────────────────────────────────────────

  openEditModal(): void {
    if (!this.item) return;
    this.editForm = {
      title: this.item.title ?? '',
      description: this.item.description ?? '',
      condition: (this.item.condition ?? 'Good') as unknown as ItemDTO.UpdateItemDTO['condition'],
      currentValue: this.item.currentValue ?? 0,
      availableFrom: this.toDateInputValue(this.item.availableFrom),
      availableUntil: this.toDateInputValue(this.item.availableUntil),
      minLoanDays: this.item.minLoanDays ?? undefined,
      pickupAddress: this.item.pickupAddress ?? '',
      pickupLatitude: this.item.pickupLatitude ?? 0,
      pickupLongitude: this.item.pickupLongitude ?? 0,
      requiresVerification: this.item.requiresVerification ?? false,
      isActive: this.item.isActive ?? true,
      photoUrl: this.item.photos?.[0]?.photoUrl ?? '',
    };
    this.editError = '';
    this.editSuccess = '';
    this.descriptionExpanded = false;
    this.addressSuggestions = [];
    this.showAddressSuggestions = false;
    this.showEditModal = true;
  }

  closeEditModal(): void {
    if (this.isSavingEdit) return;
    this.showEditModal = false;
    this.showAddressSuggestions = false;
  }

  saveEdit(): void {
    if (!this.item) return;
    if (!this.editForm.title?.trim()) {
      this.editError = 'Title is required.';
      return;
    }

    this.isSavingEdit = true;
    this.editError = '';
    this.editSuccess = '';

    this.itemService.update(this.item.id, {
      title: this.editForm.title.trim(),
      description: this.editForm.description,
      condition: this.editForm.condition,
      currentValue: this.editForm.currentValue,
      availableFrom: this.editForm.availableFrom,
      availableUntil: this.editForm.availableUntil,
      minLoanDays: this.editForm.minLoanDays,
      pickupAddress: this.editForm.pickupAddress,
      pickupLatitude: this.editForm.pickupLatitude,
      pickupLongitude: this.editForm.pickupLongitude,
      requiresVerification: this.editForm.requiresVerification,
      isActive: this.editForm.isActive,
    }).subscribe({
      next: (updated) => {
        this.item = updated;
        this.updatePhoto(updated);
      },
      error: (err) => {
        this.editError = err.error?.message ?? 'Failed to save changes.';
        this.isSavingEdit = false;
        this.cdr.detectChanges();
      }
    });
  }

  private updatePhoto(updated: ItemDTO.ItemDetailDTO): void {
    const newUrl = this.editForm.photoUrl?.trim();
    const existingPhoto = updated.photos?.[0];

    const finish = (item?: ItemDTO.ItemDetailDTO) => {
      if (item) this.item = item;
      this.isSavingEdit = false;
      this.editSuccess = '✓ Changes saved!';
      this.cdr.detectChanges();
      setTimeout(() => {
        this.showEditModal = false;
        this.editSuccess = '';
        this.cdr.detectChanges();
      }, 1200);
    };

    const addNew = () => {
      if (!newUrl) { finish(); return; }
      this.itemService.addPhoto(updated.id, { photoUrl: newUrl, isPrimary: true, displayOrder: 0 }).subscribe({
        next: () => this.itemService.getById(updated.id).subscribe({ next: finish, error: () => finish() }),
        error: () => finish(),
      });
    };

    if (existingPhoto && newUrl !== existingPhoto.photoUrl) {
      // Delete old photo first, then add new one (if provided)
      this.itemService.deletePhoto(updated.id, existingPhoto.id).subscribe({
        next: addNew,
        error: addNew, // still try to add even if delete fails
      });
    } else if (!existingPhoto && newUrl) {
      // No existing photo, just add
      addNew();
    } else {
      // No change needed
      finish();
    }
  }

  // ── Address autocomplete ────────────────────────────────────────────────────

  onAddressInput(value: string): void {
    clearTimeout(this.addressSearchTimeout);
    this.showAddressSuggestions = false;
    if (!value || value.length < 3) { this.addressSuggestions = []; return; }
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

  selectAddress(suggestion: any): void {
    this.editForm.pickupAddress = suggestion.display_name;
    this.editForm.pickupLatitude = parseFloat(suggestion.lat);
    this.editForm.pickupLongitude = parseFloat(suggestion.lon);
    this.showAddressSuggestions = false;
    this.addressSuggestions = [];
    this.cdr.detectChanges();
  }

  // ── Misc helpers ────────────────────────────────────────────────────────────

  private toDateInputValue(value: string | Date | null | undefined): string {
    if (!value) return '';
    const d = new Date(value);
    if (isNaN(d.getTime())) return '';
    return d.toISOString().slice(0, 10);
  }

  get hasActiveLoanRequest(): boolean {
    if (!this.existingLoan) return false;
    const activeStatuses = ['Pending', 'AdminPending', 'Approved', 'Active', 'Late'];
    return activeStatuses.includes(this.existingLoan.status);
  }

  requestLoan(): void {
    if (!this.item || !this.loanForm.startDate || !this.loanForm.endDate) {
      this.loanError = 'Please fill in both dates.';
      return;
    }
    this.isRequesting = true;
    this.loanError = '';

    this.loanService.createLoan({
      itemId: this.item.id,
      startDate: this.loanForm.startDate,
      endDate: this.loanForm.endDate
    }).subscribe({
      next: (loan) => {
        this.isRequesting = false;
        this.loanSuccess = '✓ Loan request sent!';
        this.cdr.detectChanges();
        setTimeout(() => {
          this.showLoanModal = false;
          this.router.navigate(['/loans', loan.id]);
        }, 1000);
      },
      error: (err) => {
        this.loanError = err.error?.message ?? 'Failed to send request.';
        this.isRequesting = false;
        this.cdr.detectChanges();
      }
    });
  }

  cancelLoan(): void {
    if (!this.existingLoan) return;
    this.isCancellingLoan = true;
    this.cancelError = '';

    this.loanService.cancelLoan(this.existingLoan.id, { reason: '' }).subscribe({
      next: () => {
        this.existingLoan = null;
        this.isCancellingLoan = false;
        this.loadItem(this.item!.id);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.cancelError = err.error?.message ?? 'Failed to cancel loan.';
        this.isCancellingLoan = false;
        this.cdr.detectChanges();
      }
    });
  }

  getCategoryEmoji(cat: string): string {
    return this.emojiMap[cat?.toLowerCase()] ?? '📦';
  }

  getConditionClass(condition: string): string {
    switch (condition?.toLowerCase()) {
      case 'excellent': return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'good': return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case 'fair': return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'poor': return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default: return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }

  goBack(): void {
    window.history.back();
  }

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active': return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'approved': return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'returned': return 'bg-cyan-600 text-white border-zinc-400/20';
      case 'late': return 'bg-red-500/10 text-red-400 border-red-500/20';
      case 'pending': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'adminpending': return 'bg-indigo-400/10 text-indigo-400 border-indigo-400/20';
      case 'cancelled': return 'bg-zinc-500/50 text-white border-zinc-600/50';
      case 'rejected': return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default: return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }


  get displayedDisputes() { return this.disputeHistory.slice(0, this.visibleDisputes); }
  get displayedLoanHistory() { return this.loanHistory.slice(0, this.visibleLoans); }
  loadMoreDisputes() { this.visibleDisputes += 5; }
  loadMoreLoanHistory() { this.visibleLoans += 5; }

}