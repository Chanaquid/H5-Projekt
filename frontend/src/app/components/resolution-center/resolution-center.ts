import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/auth-service';
import { Dispute } from "../dispute/dispute";

@Component({
  selector: 'app-resolution-center',
  imports: [CommonModule, FormsModule, Navbar, Dispute],
  templateUrl: './resolution-center.html',
  styleUrl: './resolution-center.css',
})
export class ResolutionCenter implements OnInit {
private readonly base = 'https://localhost:7183';
 
  activeTab: 'disputes' | 'appeals' | 'verification' = 'disputes';
 
  // ── Data ──
  disputes:   any[] = [];
  appeals:    any[] = [];
  verificationRequest: any = null;
  isVerified = false;
 
  // ── Loading flags ──
  isLoadingDisputes    = false;
  isLoadingAppeals     = false;
  isLoadingVerification = false;
 
  // ── Modals ──
  showDisputeModal = false;
  showAppealModal  = false;
  showVerifyModal  = false;
 
  // ── Dispute form ──
  disputeForm = { loanId: null as number | null, reason: '', description: '' };
  disputeError        = '';
  isSubmittingDispute = false;
 
  disputeReasons = [
    { label: '📦 Item Not Returned',  value: 'ItemNotReturned' },
    { label: '💥 Item Damaged',        value: 'ItemDamaged' },
    { label: '🚫 Item Not Received',   value: 'ItemNotReceived' },
    { label: '⚠️ Other',               value: 'Other' },
  ];
 
  // ── Appeal form ──
  appealForm = { fineId: null as number | null, reason: '' };
  appealError        = '';
  isSubmittingAppeal = false;
 
  // ── Verification form ──
  verifyForm  = { documentUrl: '', documentType: '' };
  verifyError        = '';
  isSubmittingVerify = false;
 
  documentTypes = [
    { label: '🪪 National ID',       value: 'NationalId' },
    { label: '🛂 Passport',          value: 'Passport' },
    { label: '🚗 Driving Licence',   value: 'DrivingLicense' },
  ];
 
  tabs: { key: 'disputes' | 'appeals' | 'verification', label: string, icon: string, count: number | null }[] = [
    { key: 'disputes',     label: 'Disputes',     icon: '⚖️', count: null },
    { key: 'appeals',      label: 'Appeals',      icon: '📣', count: null },
    { key: 'verification', label: 'Verification', icon: '🪪', count: null },
  ];
 
  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
  ) {}
 
  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) { this.router.navigate(['/']); return; }

    this.route.queryParams.subscribe(params => {
      if (params['tab']) this.activeTab = params['tab'];
    });
    this.loadDisputes();
    this.loadVerification();
  }
 
  onTabChange(): void {
    if (this.activeTab === 'disputes'     && !this.disputes.length)            this.loadDisputes();
    if (this.activeTab === 'appeals'      && !this.appeals.length)             this.loadAppeals();
    if (this.activeTab === 'verification' && !this.verificationRequest)        this.loadVerification();
  }
 
  // ─── Disputes ───────────────────────────────────────────
 
  private loadDisputes(): void {
    this.isLoadingDisputes = true;
    this.http.get<any[]>(`${this.base}/api/disputes/my`).subscribe({
      next: (data) => {
        this.disputes = data;
        this.tabs[0].count = data.length;
        this.isLoadingDisputes = false;
        this.cdr.detectChanges();
      },
      error: () => { this.isLoadingDisputes = false; this.cdr.detectChanges(); }
    });
  }
 
  submitDispute(): void {
    if (!this.disputeForm.loanId || !this.disputeForm.reason || !this.disputeForm.description) return;
    this.isSubmittingDispute = true;
    this.disputeError = '';
 
    this.http.post(`${this.base}/api/disputes`, {
      loanId:      this.disputeForm.loanId,
      reason:      this.disputeForm.reason,
      description: this.disputeForm.description,
    }).subscribe({
      next: () => {
        this.isSubmittingDispute = false;
        this.showDisputeModal    = false;
        this.disputeForm         = { loanId: null, reason: '', description: '' };
        this.disputes            = []; // force reload
        this.loadDisputes();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.disputeError        = err.error?.message ?? 'Failed to submit dispute.';
        this.isSubmittingDispute = false;
        this.cdr.detectChanges();
      }
    });
  }
 
  // ─── Appeals ────────────────────────────────────────────
 
  private loadAppeals(): void {
    this.isLoadingAppeals = true;
    this.http.get<any[]>(`${this.base}/api/appeals/my`).subscribe({
      next: (data) => {
        this.appeals = data;
        this.tabs[1].count = data.length;
        this.isLoadingAppeals = false;
        this.cdr.detectChanges();
      },
      error: () => { this.isLoadingAppeals = false; this.cdr.detectChanges(); }
    });
  }
 
  submitAppeal(): void {
    if (!this.appealForm.fineId || !this.appealForm.reason) return;
    this.isSubmittingAppeal = true;
    this.appealError = '';
 
    this.http.post(`${this.base}/api/appeals`, {
      fineId: this.appealForm.fineId,
      reason: this.appealForm.reason,
    }).subscribe({
      next: () => {
        this.isSubmittingAppeal = false;
        this.showAppealModal    = false;
        this.appealForm         = { fineId: null, reason: '' };
        this.appeals            = [];
        this.loadAppeals();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.appealError        = err.error?.message ?? 'Failed to submit appeal.';
        this.isSubmittingAppeal = false;
        this.cdr.detectChanges();
      }
    });
  }
 
  // ─── Verification ────────────────────────────────────────
 
  private loadVerification(): void {
    this.isLoadingVerification = true;
 
    // Check JWT claim first for isVerified
    const token = this.authService.getToken();
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        this.isVerified = payload['isVerified'] === 'true' || payload['isVerified'] === true;
      } catch { this.isVerified = false; }
    }
 
    if (this.isVerified) {
      // Still load the request for "verified on" date
      this.http.get<any>(`${this.base}/api/verification/my`).subscribe({
        next: (req) => {
          this.verificationRequest   = req;
          this.isLoadingVerification = false;
          this.cdr.detectChanges();
        },
        error: () => { this.isLoadingVerification = false; this.cdr.detectChanges(); }
      });
      return;
    }
 
    this.http.get<any>(`${this.base}/api/verification/my`).subscribe({
      next: (req) => {
        this.verificationRequest   = req;
        this.isLoadingVerification = false;
        this.cdr.detectChanges();
      },
      error: () => {
        // 404 = no request yet
        this.verificationRequest   = null;
        this.isLoadingVerification = false;
        this.cdr.detectChanges();
      }
    });
  }
 
  submitVerification(): void {
    if (!this.verifyForm.documentUrl || !this.verifyForm.documentType) return;
    this.isSubmittingVerify = true;
    this.verifyError = '';
 
    this.http.post(`${this.base}/api/verification`, this.verifyForm).subscribe({
      next: (res: any) => {
        this.verificationRequest = res;
        this.isSubmittingVerify  = false;
        this.showVerifyModal     = false;
        this.verifyForm          = { documentUrl: '', documentType: '' };
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.verifyError        = err.error?.message ?? 'Failed to submit verification.';
        this.isSubmittingVerify = false;
        this.cdr.detectChanges();
      }
    });
  }
 
  // ─── Status class helpers ────────────────────────────────
 
  getDisputeStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'open':       return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'inreview':
      case 'in review':  return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'resolved':   return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'closed':     return 'bg-zinc-700 text-zinc-400 border-zinc-600';
      default:           return 'bg-zinc-700 text-zinc-400 border-zinc-600';
    }
  }
 
  getAppealStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'pending':    return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'approved':   return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'rejected':   return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default:           return 'bg-zinc-700 text-zinc-400 border-zinc-600';
    }
  }
}
