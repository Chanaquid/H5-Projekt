import { AfterViewChecked, ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { LoanService } from '../../services/loan-service';
import { LoanMessageService } from '../../services/loan-message-service';
import { UserService } from '../../services/user-service';
import { ReviewService } from '../../services/review-service';
import { ItemService } from '../../services/item-service';
import { DisputeService } from '../../services/dispute-service';
import { SignalRService } from '../../services/signal-r-service';
import { LoanDTO } from '../../dtos/loanDTO';
import { ChatDTO } from '../../dtos/chatDTO';
import { UserDTO } from '../../dtos/userDTO';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-loan-details',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './loan-details.html',
  styleUrl: './loan-details.css',
})
export class LoanDetails implements OnInit, OnDestroy {

  @ViewChild('messageContainer') messageContainer!: ElementRef;

  loan: LoanDTO.LoanDetailDTO | null = null;
  thread: ChatDTO.LoanMessageDTO.LoanMessageThreadDTO | null = null;
  currentUserId = '';
  isLoading = true;
  isSending = false;
  newMessage = '';
  selectedPhoto: string | null = null;
  private loanId = 0;

  isAdmin = false;

  //qr code
  showQrModal = false;
  qrCode = '';
  isLoadingQr = false;


  // Decision
  decisionNote = '';
  decisionError = '';
  isDeciding = false;

  // Reviews
  showReviewItem = false;
  showReviewUser = false;
  itemReviewRating = 0;
  itemReviewComment = '';
  userReviewRating = 0;
  userReviewComment = '';
  isSubmittingItemReview = false;
  isSubmittingUserReview = false;
  itemReviewError = '';
  itemReviewSuccess = '';
  userReviewError = '';
  userReviewSuccess = '';
  hasReviewedItem = false;
  hasReviewedUser = false;

  // Loan activation
  isActivatingLoan = false;
  isCompletingLoan = false;
  activateError = '';
  completeError = '';
  qrCodeInput = '';

  //loan cancel
    isCancellingLoan = false;
  cancelLoanError = '';
  showCancelConfirm = false;


  // Dispute
  showDisputeModal = false;
  disputeForm = { description: '', photoUrl: '', photoCaption: '' };
  isFilingDispute = false;
  disputeError = '';

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private authService: AuthService,
    private loanService: LoanService,
    private loanMessageService: LoanMessageService,
    private userService: UserService,
    private reviewService: ReviewService,
    private itemService: ItemService,
    private disputeService: DisputeService,
    private signalRService: SignalRService,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    this.loanId = Number(this.route.snapshot.paramMap.get('id'));

    this.userService.getMe().subscribe({
      next: (user) => {
        this.currentUserId = user.id;
        this.loadLoan(this.loanId);
      },
      error: () => {
        this.loadLoan(this.loanId);
      }
    });
  }

  ngOnDestroy(): void {
    this.signalRService.leaveLoanGroup(this.loanId);
    this.signalRService.offReceiveMessage();
    this.signalRService.offMessagesRead();
  }

  private startSignalR(loanId: number): void {
    // Register handlers first regardless of connection state
    this.signalRService.onReceiveMessage((msg) => {
      if (!this.thread) {
        this.thread = {
          loanId,
          itemTitle: this.loan?.item.title ?? '',
          otherPartyName: '',
          otherPartyAvatarUrl: undefined,
          messages: []
        };
      }
      const exists = this.thread.messages.some(m => m.id === msg.id);
      if (!exists) {
        this.thread.messages.push(msg);
        this.cdr.detectChanges();
        this.triggerScroll();
        if (msg.senderId !== this.currentUserId) {
          this.loanMessageService.markThreadAsRead(loanId).subscribe();
        }
      }
      this.cdr.detectChanges();
    });

    this.signalRService.onMessagesRead((data) => {
      const id = data.loanId ?? data.LoanId;
      if (this.thread && id === loanId) {
        this.thread.messages.forEach(m => {
          if (m.senderId === this.currentUserId) m.isRead = true;
        });
        this.cdr.detectChanges();
      }
    });

    // If already connected, just join the group
    if (this.signalRService.isConnected) {
      this.signalRService.joinLoanGroup(loanId).catch(err =>
        console.warn('Could not join loan group:', err)
      );
      return;
    }

    // Otherwise start and then join
    this.signalRService.startConnection().then(() => {
      this.signalRService.joinLoanGroup(loanId).catch(err =>
        console.warn('Could not join loan group:', err)
      ); 
    }).catch(err => console.error('SignalR connection failed:', err));
  }

  private loadLoan(id: number): void {
    this.loanService.getById(id).subscribe({
      next: (loan) => {
        this.loan = loan;
        this.isLoading = false;
        this.cdr.detectChanges();
        this.loadThread(id);
        this.startSignalR(id);
        if (loan.status === 'Returned' || loan.status === 'Late') {
          this.checkExistingReviews(id);
        }
      },
      error: () => {
        this.loan = null;
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadThread(loanId: number): void {
    this.loanMessageService.getThread(loanId).subscribe({
      next: (thread) => {
        this.thread = thread;
        this.cdr.detectChanges();
        this.triggerScroll();
      },
      error: () => { }
    });
  }

  get isChatLocked(): boolean {
    if (!this.loan) return false;
    const terminalStatuses = ['Returned', 'Cancelled', 'Rejected'];
    if (!terminalStatuses.includes(this.loan.status)) return false;
    const lockedAt = this.loan.actualReturnDate ?? this.loan.createdAt;
    return new Date(lockedAt) < new Date(Date.now() - 7 * 24 * 60 * 60 * 1000);
  }

  private checkExistingReviews(loanId: number): void {
    if (!this.loan) return;

    this.reviewService.getItemReviews(this.loan.item.id).subscribe({
      next: (reviews) => {
        this.hasReviewedItem = reviews.some((r: any) => r.loanId === loanId);
        this.cdr.detectChanges();
      },
      error: () => { }
    });

    if (this.otherParty?.id) {
      this.reviewService.getUserReviews(this.otherParty.id).subscribe({
        next: (reviews) => {
          this.hasReviewedUser = reviews.some((r: any) => r.loanId === loanId);
          this.cdr.detectChanges();
        },
        error: () => { }
      });
    }
  }

  private triggerScroll(): void {
    setTimeout(() => {
      this.scrollToBottom();
      this.cdr.detectChanges();
    }, 50);
  }

  private scrollToBottom(): void {
    try {
      const el = this.messageContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    } catch { }
  }

  trackMessage(_index: number, msg: any): number {
    return msg.id;
  }

  openQrModal(): void {
    this.showQrModal = true;
    if (this.qrCode) return; // already loaded
    this.isLoadingQr = true;

    this.itemService.getQrCode(this.loan!.item.id).subscribe({
      next: (res) => {
        this.qrCode = res.qrCode;
        this.isLoadingQr = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingQr = false;
        this.cdr.detectChanges();
      }
    });
  }

  decide(isApproved: boolean): void {
    if (!this.loan) return;
    this.isDeciding = true;
    this.decisionError = '';

    this.loanService.decideLoan(this.loan.id, { isApproved, decisionNote: this.decisionNote }).subscribe({
      next: (updated) => {
        this.loan = updated;
        this.isDeciding = false;
        this.decisionNote = '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.decisionError = err.error?.message ?? 'Failed to process decision.';
        this.isDeciding = false;
        this.cdr.detectChanges();
      }
    });
  }

  sendMessage(): void {
    if (!this.newMessage.trim() || !this.loan || this.isSending) return;
    this.isSending = true;
    const content = this.newMessage.trim();
    this.newMessage = '';

    this.loanMessageService.send({ loanId: this.loan.id, content }).subscribe({
      next: () => {
        // SignalR delivers the message via ReceiveMessage — no manual push needed
        this.isSending = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.newMessage = content;
        this.isSending = false;
        this.cdr.detectChanges();
      }
    });
  }

  activateLoan(): void {
    if (!this.qrCodeInput.trim()) return;
    this.isActivatingLoan = true;
    this.activateError = '';

    this.itemService.scan(this.qrCodeInput.trim().toUpperCase()).subscribe({
      next: () => {
        this.isActivatingLoan = false;
        this.qrCodeInput = '';
        this.loadLoan(this.loan!.id);
      },
      error: (err) => {
        this.activateError = err.error?.message ?? 'Invalid QR code.';
        this.isActivatingLoan = false;
        this.cdr.detectChanges();
      }
    });
  }

  cancelLoan(): void {
    if (!this.loan) return;
    this.isCancellingLoan = true;
    this.cancelLoanError = '';

    this.loanService.cancelLoan(this.loan.id, { reason: '' }).subscribe({
      next: () => {
        this.isCancellingLoan  = false;
        this.showCancelConfirm = false;
        this.loadLoan(this.loan!.id);
      },
      error: (err) => {
        this.cancelLoanError   = err.error?.message ?? 'Failed to cancel loan.';
        this.isCancellingLoan  = false;
        this.cdr.detectChanges();
      }
    });
  }

  completeLoan(): void {
    if (!this.qrCodeInput.trim()) return;
    this.isCompletingLoan = true;
    this.completeError = '';

    this.itemService.scan(this.qrCodeInput.trim().toUpperCase()).subscribe({
      next: () => {
        this.isCompletingLoan = false;
        this.qrCodeInput = '';
        this.loadLoan(this.loan!.id);
      },
      error: (err) => {
        this.completeError = err.error?.message ?? 'Invalid QR code.';
        this.isCompletingLoan = false;
        this.cdr.detectChanges();
      }
    });
  }

  fileDispute(): void {
    if (!this.loan || !this.disputeForm.description.trim()) {
      this.disputeError = 'Please describe the issue.';
      return;
    }
    this.isFilingDispute = true;
    this.disputeError = '';

    const filedAs = this.effectiveRole === 'Owner' ? 'AsOwner' : 'AsBorrower';

    this.disputeService.create({
      loanId: this.loan.id,
      filedAs,
      description: this.disputeForm.description.trim()
    }).subscribe({
      next: (created) => {
        // If a photo was provided, upload it
        if (this.disputeForm.photoUrl.trim()) {
          this.disputeService.addPhoto(created.id, {
            photoUrl: this.disputeForm.photoUrl.trim(),
            caption: this.disputeForm.photoCaption.trim() || undefined
          }).subscribe({
            next: () => this.onDisputeSuccess(),
            error: () => this.onDisputeSuccess() // photo failed but dispute was created, proceed
          });
        } else {
          this.onDisputeSuccess();
        }
      },
      error: (err) => {
        this.disputeError = err.error?.message ?? 'Failed to file dispute.';
        this.isFilingDispute = false;
        this.cdr.detectChanges();
      }
    });
  }

  private onDisputeSuccess(): void {
    this.isFilingDispute = false;
    this.showDisputeModal = false;
    this.disputeForm = { description: '', photoUrl: '', photoCaption: '' };
    this.cdr.detectChanges(); // ← add this
    this.loadLoan(this.loan!.id);
  }

  setItemRating(r: number): void { this.itemReviewRating = r; }

  setUserRating(r: number): void { this.userReviewRating = r; }

  submitItemReview(): void {
    if (!this.loan || this.itemReviewRating === 0) {
      this.itemReviewError = 'Please select a rating.';
      return;
    }
    this.isSubmittingItemReview = true;
    this.itemReviewError = '';

    this.reviewService.createItemReview({
      loanId: this.loan.id,
      itemId: this.loan.item.id,
      rating: this.itemReviewRating,
      comment: this.itemReviewComment.trim() || undefined
    }).subscribe({
      next: () => {
        this.hasReviewedItem = true;
        this.isSubmittingItemReview = false;
        this.itemReviewSuccess = '✓ Item review submitted!';
        this.showReviewItem = false;
        this.itemReviewRating = 0;
        this.itemReviewComment = '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.itemReviewError = err.error?.message ?? 'Failed to submit review.';
        this.isSubmittingItemReview = false;
        this.cdr.detectChanges();
      }
    });
  }

  submitUserReview(): void {
    if (!this.loan || this.userReviewRating === 0) {
      this.userReviewError = 'Please select a rating.';
      return;
    }
    this.isSubmittingUserReview = true;
    this.userReviewError = '';

    this.reviewService.createUserReview({
      loanId: this.loan.id,
      reviewedUserId: this.otherParty?.id ?? '',
      rating: this.userReviewRating,
      comment: this.userReviewComment.trim() || undefined
    }).subscribe({
      next: () => {
        this.hasReviewedUser = true;
        this.isSubmittingUserReview = false;
        this.userReviewSuccess = '✓ User review submitted!';
        this.showReviewUser = false;
        this.userReviewRating = 0;
        this.userReviewComment = '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.userReviewError = err.error?.message ?? 'Failed to submit review.';
        this.isSubmittingUserReview = false;
        this.cdr.detectChanges();
      }
    });
  }

  get isOwner(): boolean {
    return this.loan?.owner?.id === this.currentUserId;
  }

  get otherParty(): UserDTO.UserSummaryDTO | null {
    if (!this.loan) return null;
    // console.log("Other party", this.isOwner ? this.loan.borrower : this.loan.owner);
    return this.isOwner ? this.loan.borrower : this.loan.owner;
  }

  get effectiveRole(): 'Admin' | 'Owner' | 'Borrower' {
    if (this.isOwner) return 'Owner';
    if (this.loan?.borrower?.id === this.currentUserId) return 'Borrower';
    if (this.isAdmin) return 'Admin';
    return 'Borrower';
  }

  goToItem(): void {
    if (this.loan) this.router.navigate(['/items', this.loan.item.id]);
  }

  openPhoto(url: string): void {
    this.selectedPhoto = url;
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active': return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'approved': return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'returned': return 'bg-cyan-300/10 text-cyan-300 border-cyan-300/20';
      case 'late':
      case 'overdue': return 'bg-red-400/10 text-red-400 border-red-400/20';
      case 'pending': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'adminpending': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';

      case 'cancelled':
      case 'rejected': return 'bg-rose-400/10 text-rose-400 border-rose-400/20';
      default: return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
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

  get isDisputeLocked(): boolean {
    if (!this.loan) return false;
    if (this.loan.status !== 'Returned') return false;
    const completedAt = this.loan.actualReturnDate ?? this.loan.updatedAt;
    if (!completedAt) return false;
    return new Date(completedAt) < new Date(Date.now() - 7 * 24 * 60 * 60 * 1000);
  }



}