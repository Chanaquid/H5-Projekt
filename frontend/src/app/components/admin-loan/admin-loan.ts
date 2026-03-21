import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { LoanService } from '../../services/loan-service';
import { LoanDTO } from '../../dtos/loanDTO';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-admin-loan',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './admin-loan.html',
  styleUrl: './admin-loan.css',
})
export class AdminLoan implements OnInit {

  allLoans: LoanDTO.LoanSummaryDTO[] = [];
  filteredLoans: LoanDTO.LoanSummaryDTO[] = [];
  isLoading = true;
  searchQuery = '';
  activeTab: 'all' | 'pending' | 'adminPending' | 'approved' | 'active' | 'late' | 'returned' | 'cancelled' = 'pending';

  // Modal
  showLoanModal = false;
  isLoadingDetail = false;
  selectedLoan: LoanDTO.LoanSummaryDTO | null = null;
  loanDetail: LoanDTO.LoanDetailDTO | null = null;
  selectedPhoto: string | null = null;

  // Admin decision
  adminDecisionNote = '';
  decisionError = '';
  decisionSuccess = '';
  isDeciding = false;

  tabs = [
    { key: 'all' as const, label: 'All' },
    { key: 'pending' as const, label: 'Pending' },
    { key: 'adminPending' as const, label: 'Admin Pending' },
    { key: 'approved' as const, label: 'Approved' },
    { key: 'active' as const, label: 'Active' },
    { key: 'late' as const, label: 'Late' },
    { key: 'returned' as const, label: 'Returned' },
    { key: 'cancelled' as const, label: 'Cancelled' },
  ];

  constructor(
    private authService: AuthService,
    private loanService: LoanService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }
    this.loadLoans();
  }

  // ─── Load ────────────────────────────────────────────────────────────────

  private loadLoans(): void {
    this.isLoading = true;
    this.loanService.getAllLoans().subscribe({
      next: (loans) => {
        this.allLoans = loans;
        console.log(this.allLoans)
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
    let result = [...this.allLoans];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(l =>
        l.itemTitle.toLowerCase().includes(q) ||
        l.otherPartyName.toLowerCase().includes(q)
      );
    }

    if (this.activeTab !== 'all') {
      // Map tab key to actual status string
      const statusMap: Record<string, string> = {
        pending: 'Pending',
        adminPending: 'AdminPending',
        approved: 'Approved',
        active: 'Active',
        late: 'Late',
        returned: 'Returned',
        cancelled: 'Cancelled',
      };
      const targetStatus = statusMap[this.activeTab];
      if (targetStatus) {
        result = result.filter(l => l.status === targetStatus);
      }
    }

    // Sort — late first, then by most recent
    result.sort((a, b) => {
      if (a.status === 'Late' && b.status !== 'Late') return -1;
      if (b.status === 'Late' && a.status !== 'Late') return 1;
      return new Date(b.startDate).getTime() - new Date(a.startDate).getTime();
    });

    this.filteredLoans = result;
    this.cdr.detectChanges();
  }

  getTabCount(key: string): number {
    const statusMap: Record<string, string> = {
      all: '',
      pending: 'Pending',
      adminPending: 'AdminPending',
      approved: 'Approved',
      active: 'Active',
      late: 'Late',
      returned: 'Returned',
      cancelled: 'Cancelled',
    };
    if (key === 'all') return this.allLoans.length;
    return this.allLoans.filter(l => l.status === statusMap[key]).length;
  }

  get adminPendingCount() { return this.allLoans.filter(l => l.status === 'AdminPending').length; }
  get pendingCount() { return this.allLoans.filter(l => l.status === 'Pending').length; }
  get approvedCount() { return this.allLoans.filter(l => l.status === 'Approved').length; }
  get activeCount() { return this.allLoans.filter(l => l.status === 'Active').length; }
  get lateCount() { return this.allLoans.filter(l => l.status === 'Late').length; }
  get returnedCount() { return this.allLoans.filter(l => l.status === 'Returned').length; }

  // ─── Modal ───────────────────────────────────────────────────────────────

  openLoanModal(loan: LoanDTO.LoanSummaryDTO): void {
    this.selectedLoan = loan;
    this.loanDetail = null;
    this.showLoanModal = true;
    this.isLoadingDetail = true;
    this.adminDecisionNote = '';
    this.decisionError = '';
    this.decisionSuccess = '';
    this.selectedPhoto = null;

    this.loanService.getById(loan.id).subscribe({
      next: (detail) => {
        this.loanDetail = detail;
        this.isLoadingDetail = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingDetail = false;
        this.showLoanModal = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ─── Whether this loan needs admin approval (borrower has low score etc.) ──

  get requiresAdminApproval(): boolean {
    return this.loanDetail?.status === 'AdminPending' || this.loanDetail?.status === 'Pending';
  }

  // ─── Admin decision ──────────────────────────────────────────────────────

  adminDecide(isApproved: boolean): void {
  if (!this.loanDetail) return;
  this.isDeciding    = true;
  this.decisionError = '';

  const isAdminPending = this.loanDetail.status === 'AdminPending';

  const request$ = isAdminPending
    ? this.loanService.adminDecide(this.loanDetail.id, {
        isApproved,
        decisionNote: this.adminDecisionNote.trim() || undefined
      })
    : this.loanService.decideLoan(this.loanDetail.id, {
        isApproved,
        decisionNote: this.adminDecisionNote.trim() || undefined
      });

  request$.subscribe({
    next: (updated) => {
      this.loanDetail = updated;
      this.isDeciding = false;
      this.decisionSuccess = isApproved ? 'Loan approved.' : 'Loan rejected.';
      const idx = this.allLoans.findIndex(l => l.id === updated.id);
      if (idx !== -1) this.allLoans[idx].status = updated.status;
      this.applyFilters();
      this.cdr.detectChanges();
      setTimeout(() => {
        this.showLoanModal   = false;
        this.decisionSuccess = '';
        this.cdr.detectChanges();
      }, 1500);
    },
    error: (err) => {
      this.decisionError = err.error?.message ?? 'Failed to process decision.';
      this.isDeciding    = false;
      this.cdr.detectChanges();
    }
  });
}

  // ─── Style helpers ───────────────────────────────────────────────────────

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active': return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'approved': return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'returned': return 'bg-cyan-600 text-white border-zinc-400/20';
      case 'late': return 'bg-red-500/10 text-red-400 border-red-500/20';
      case 'pending': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'adminpending': return 'bg-indigo-400/10 text-indigo-400 border-indigo-400/20';
      case 'cancelled': return 'bg-zinc-700/50 text-zinc-500 border-zinc-600/50';
      case 'rejected': return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
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

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }


}
