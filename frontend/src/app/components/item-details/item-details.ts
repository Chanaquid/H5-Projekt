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
        this.cdr.detectChanges();
        this.loadReviews(id);
        if (this.authService.isLoggedIn() && !this.isOwner) {
          this.checkExistingLoan();
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
              // Only set if it's for this specific item
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
        //Reload item to refresh isCurrentlyOnLoan
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
      case 'pending':
      case 'adminpending': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'late': return 'bg-red-400/10 text-red-400 border-red-400/20';
      case 'cancelled':
      case 'rejected': return 'bg-zinc-700 text-zinc-400 border-zinc-600';
      default: return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }
}