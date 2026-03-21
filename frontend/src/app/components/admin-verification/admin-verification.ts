import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { VerificationService } from '../../services/verification-service';
import { FineService } from '../../services/fine-service';
import { VerificationDTO } from '../../dtos/verificationDTO';
import { FineDTO } from '../../dtos/fineDTO';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-admin-verification',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './admin-verification.html',
  styleUrl: './admin-verification.css',
})
export class AdminVerification implements OnInit {

  activeTab: 'user' | 'payment' = 'user';

  // User verifications
  allUserVerifications: VerificationDTO.VerificationRequestResponseDTO[] = [];
  isLoadingUser = true;
  userVerificationFilter: 'all' | 'Pending' | 'Approved' | 'Rejected' = 'Pending';

  // Payment verifications
  allPaymentVerifications: FineDTO.FineResponseDTO[] = [];
  isLoadingPayment = true;
  paymentVerificationFilter: 'all' | 'PendingVerification' | 'Paid' | 'Unpaid' = 'PendingVerification';

  // Decision modal — user verification
  showUserDecisionModal = false;
  selectedVerification: VerificationDTO.VerificationRequestResponseDTO | null = null;
  userDecisionNote = '';
  isDecidingUser = false;
  userDecisionError = '';

  // Decision modal — fine payment
  showPaymentDecisionModal = false;
  selectedFine: FineDTO.FineResponseDTO | null = null;
  paymentDecisionNote = '';
  isDecidingPayment = false;
  paymentDecisionError = '';

  // Lightbox
  lightboxUrl: string | null = null;

  constructor(
    private authService: AuthService,
    private verificationService: VerificationService,
    private fineService: FineService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }
    this.loadUserVerifications();
    this.loadPaymentVerifications();
  }

  // ─── Load ────────────────────────────────────────────────────────────────

  private loadUserVerifications(): void {
    this.isLoadingUser = true;
    this.verificationService.getAllRequests().subscribe({
      next: (data) => {
        this.allUserVerifications = data;
        this.isLoadingUser = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingUser = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadPaymentVerifications(): void {
    this.isLoadingPayment = true;
    this.fineService.getAllAdmin().subscribe({
      next: (data) => {
        this.allPaymentVerifications = data;
        this.isLoadingPayment = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingPayment = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ─── Filtered getters ────────────────────────────────────────────────────

  get filteredUserVerifications(): VerificationDTO.VerificationRequestResponseDTO[] {
    if (this.userVerificationFilter === 'all') return this.allUserVerifications;
    return this.allUserVerifications.filter(v => v.status === this.userVerificationFilter);
  }

  get filteredPaymentVerifications(): FineDTO.FineResponseDTO[] {
    if (this.paymentVerificationFilter === 'all') return this.allPaymentVerifications;
    return this.allPaymentVerifications.filter(f => f.status === this.paymentVerificationFilter);
  }

  // ─── Count getters ───────────────────────────────────────────────────────

  get userPendingCount()  { return this.allUserVerifications.filter(v => v.status === 'Pending').length; }
  get userApprovedCount() { return this.allUserVerifications.filter(v => v.status === 'Approved').length; }
  get userRejectedCount() { return this.allUserVerifications.filter(v => v.status === 'Rejected').length; }

  get paymentPendingCount() { return this.allPaymentVerifications.filter(f => f.status === 'PendingVerification').length; }
  get paymentPaidCount()    { return this.allPaymentVerifications.filter(f => f.status === 'Paid').length; }
  get paymentUnpaidCount()  { return this.allPaymentVerifications.filter(f => f.status === 'Unpaid').length; }

  // ─── User verification decision ──────────────────────────────────────────

  openUserDecision(v: VerificationDTO.VerificationRequestResponseDTO): void {
    this.selectedVerification  = v;
    this.userDecisionNote      = '';
    this.userDecisionError     = '';
    this.showUserDecisionModal = true;
  }

  decideUser(isApproved: boolean): void {
    if (!this.selectedVerification) return;
    if (!isApproved && !this.userDecisionNote.trim()) {
      this.userDecisionError = 'A reason is required when rejecting.';
      return;
    }
    this.isDecidingUser    = true;
    this.userDecisionError = '';

    this.verificationService.decideRequest(this.selectedVerification.id, {
      isApproved,
      adminNote: this.userDecisionNote.trim() || undefined
    }).subscribe({
      next: () => {
        this.isDecidingUser        = false;
        this.showUserDecisionModal = false;
        this.loadUserVerifications();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.userDecisionError = err.error?.message ?? 'Failed to process decision.';
        this.isDecidingUser    = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ─── Payment verification decision ───────────────────────────────────────

  openPaymentDecision(fine: FineDTO.FineResponseDTO): void {
    this.selectedFine             = fine;
    this.paymentDecisionNote      = '';
    this.paymentDecisionError     = '';
    this.showPaymentDecisionModal = true;
  }

  decidePayment(isApproved: boolean): void {
    if (!this.selectedFine) return;
    if (!isApproved && !this.paymentDecisionNote.trim()) {
      this.paymentDecisionError = 'A reason is required when rejecting.';
      return;
    }
    this.isDecidingPayment    = true;
    this.paymentDecisionError = '';

    this.fineService.adminConfirmPayment({
      fineId:          this.selectedFine.id,
      isApproved,
      rejectionReason: isApproved ? undefined : this.paymentDecisionNote.trim()
    }).subscribe({
      next: () => {
        this.isDecidingPayment        = false;
        this.showPaymentDecisionModal = false;
        this.loadPaymentVerifications();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.paymentDecisionError = err.error?.message ?? 'Failed to process decision.';
        this.isDecidingPayment    = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  getDocTypeLabel(type: string): string {
    switch (type) {
      case 'Passport':       return '🛂 Passport';
      case 'NationalId':     return '🪪 National ID';
      case 'DrivingLicense': return '🚗 Driving License';
      default:               return type;
    }
  }

  getVerificationStatusClass(status: string): string {
    switch (status) {
      case 'Pending':  return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'Approved': return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'Rejected': return 'bg-red-400/10 text-red-400 border-red-400/20';
      default:         return 'bg-zinc-700 text-zinc-400 border-zinc-600';
    }
  }

  getFineStatusClass(status: string): string {
    switch (status) {
      case 'Unpaid':              return 'bg-red-400/10 text-red-400 border-red-400/20';
      case 'PendingVerification': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'Paid':                return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      default:                    return 'bg-zinc-700 text-zinc-400 border-zinc-600';
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}