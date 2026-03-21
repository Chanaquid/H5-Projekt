import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { DisputeDTO } from '../../dtos/disputeDTO';
import { ActivatedRoute, Router } from '@angular/router';
import { DisputeService } from '../../services/dispute-service';
import { FineService } from '../../services/fine-service';
import { UserDTO } from '../../dtos/userDTO';
import { UserService } from '../../services/user-service';

@Component({
  selector: 'app-dispute',
  imports: [CommonModule, FormsModule],
  templateUrl: './dispute.html',
  styleUrl: './dispute.css',
})
export class Dispute implements OnInit {

  currentUserId = '';

  //List
  disputes: DisputeDTO.DisputeSummaryDTO[] = [];
  isLoading = false;
  disputeFilter: 'all' | 'mine' | 'others' = 'all';
  sortBy: 'newest' | 'oldest' | 'deadline' | 'status' = 'newest';


  //Create modal
  showCreateModal = false;
  isSubmitting = false;
  createError = '';
  createForm: DisputeDTO.CreateDisputeDTO = { loanId: 0, filedAs: '', description: '' };

  filedAsOptions = [
    { label: '🏠 As Owner', value: 'AsOwner' },
    { label: '📦 As Borrower', value: 'AsBorrower' },
  ];

  //Detail modal
  showDetailModal = false;
  isLoadingDetail = false;
  selectedDispute: DisputeDTO.DisputeDetailDTO | null = null;

  //Response
  responseText = '';
  responseError = '';
  isSubmittingResponse = false;

  //Add photo
  newPhotoUrl = '';
  newPhotoCaption = '';
  photoError = '';
  isAddingPhoto = false;

  //Lightbox
  lightboxUrl: string | null = null;

  //Fines
  disputeFines: any[] = [];

  //Score
  disputeScoreHistory: UserDTO.ScoreHistoryDTO[] = [];

  constructor(
    public router: Router,
    private route: ActivatedRoute,
    private disputeService: DisputeService,
    private fineService: FineService,
    private userService: UserService,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    const token = localStorage.getItem('token');
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        this.currentUserId = payload['sub'] ?? '';
      } catch { }
    }
    this.loadDisputes();

    // Auto-open detail modal if disputeId is in query params
    this.route.queryParams.subscribe(params => {
      const disputeId = params['disputeId'];
      if (disputeId) {
        this.openDetail(Number(disputeId));
      }
    });
  }

  private loadDisputes(): void {
    this.isLoading = true;
    this.disputeService.getMyDisputes().subscribe({
      next: (data) => {
        this.disputes = data;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }


  submitCreate(): void {
    if (!this.createForm.loanId || !this.createForm.filedAs || !this.createForm.description) return;
    this.isSubmitting = true;
    this.createError = '';

    this.disputeService.create(this.createForm).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showCreateModal = false;
        this.createForm = { loanId: 0, filedAs: '', description: '' };
        this.loadDisputes();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.createError = err.error?.message ?? 'Failed to submit dispute.';
        this.isSubmitting = false;
        this.cdr.detectChanges();
      }
    });
  }


  openDetail(id: number): void {
    this.selectedDispute = null;
    this.showDetailModal = true;
    this.isLoadingDetail = true;
    this.responseText = '';
    this.responseError = '';
    this.newPhotoUrl = '';
    this.newPhotoCaption = '';
    this.photoError = '';
    this.disputeFines = [];

    this.disputeService.getById(id).subscribe({
      next: (data) => {
        this.selectedDispute = data;
        this.isLoadingDetail = false;
        if (data.status === 'Resolved') {
          this.loadDisputeFines(data.id);
          this.loadDisputeScoreHistory(data.loanId);
        }
        setTimeout(() => this.cdr.detectChanges(), 0);
      },
      error: () => {
        this.isLoadingDetail = false;
        this.showDetailModal = false;
        this.cdr.detectChanges();
      }
    });
  }

  closeDetail(): void {
    this.showDetailModal = false;
    this.selectedDispute = null;
    this.disputeFines = [];
    this.disputeScoreHistory = [];
  }

  private loadDisputeScoreHistory(loanId: number): void {
    this.userService.getScoreHistoryByLoanId(loanId).subscribe({
      next: (history) => { this.disputeScoreHistory = history; this.cdr.detectChanges(); },
      error: () => { this.disputeScoreHistory = []; }
    });
  }


  private loadDisputeFines(disputeId: number): void {
    this.fineService.getFinesByDisputeId(disputeId).subscribe({
      next: (fines) => { this.disputeFines = fines; this.cdr.detectChanges(); },
      error: () => { this.disputeFines = []; }
    });
  }


  submitResponse(): void {
    if (!this.selectedDispute || !this.responseText.trim()) return;
    this.isSubmittingResponse = true;
    this.responseError = '';

    this.disputeService.respond(this.selectedDispute.id, {
      responseDescription: this.responseText.trim()
    }).subscribe({
      next: (updated) => {
        this.selectedDispute = updated;
        this.responseText = '';
        this.isSubmittingResponse = false;
        setTimeout(() => this.cdr.detectChanges(), 0);
      },
      error: (err) => {
        this.responseError = err.error?.message ?? 'Failed to submit response.';
        this.isSubmittingResponse = false;
        this.cdr.detectChanges();
      }
    });
  }


  addPhoto(): void {
    if (!this.selectedDispute || !this.newPhotoUrl.trim()) return;
    this.isAddingPhoto = true;
    this.photoError = '';

    this.disputeService.addPhoto(this.selectedDispute.id, {
      photoUrl: this.newPhotoUrl.trim(),
      caption: this.newPhotoCaption.trim() || undefined
    }).subscribe({
      next: () => {
        this.isAddingPhoto = false;
        this.newPhotoUrl = '';
        this.newPhotoCaption = '';
        this.openDetail(this.selectedDispute!.id);
      },
      error: (err) => {
        this.photoError = err.error?.message ?? 'Failed to add photo.';
        this.isAddingPhoto = false;
        this.cdr.detectChanges();
      }
    });
  }


  get filteredDisputes(): DisputeDTO.DisputeSummaryDTO[] {
    let result = this.disputes;

    // Filter
    switch (this.disputeFilter) {
      case 'mine': result = result.filter(d => d.filedById === this.currentUserId); break;
      case 'others': result = result.filter(d => d.filedById !== this.currentUserId); break;
    }

    // Sort
    const statusOrder: Record<string, number> = {
      'AwaitingResponse': 0,
      'UnderReview': 1,
      'Resolved': 2,
    };

    switch (this.sortBy) {
      case 'newest':
        return [...result].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
      case 'oldest':
        return [...result].sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
      case 'deadline':
        return [...result].sort((a, b) => new Date(a.responseDeadline).getTime() - new Date(b.responseDeadline).getTime());
      case 'status':
        return [...result].sort((a, b) => (statusOrder[a.status] ?? 99) - (statusOrder[b.status] ?? 99));
      default:
        return result;
    }
  }

  get filterOptions() {
    return [
      { label: 'All', value: 'all' as const, count: this.disputes.length },
      { label: 'Filed by you', value: 'mine' as const, count: this.disputes.filter(d => d.filedById === this.currentUserId).length },
      { label: 'Filed by others', value: 'others' as const, count: this.disputes.filter(d => d.filedById !== this.currentUserId).length },
    ];
  }

  needsMyResponse(d: DisputeDTO.DisputeSummaryDTO): boolean {
    return d.status === 'AwaitingResponse' && d.filedById !== this.currentUserId;
  }

  isCurrentUserFiler(): boolean {
    return this.selectedDispute?.filedById === this.currentUserId;
  }

  isDeadlinePassed(deadline: string): boolean {
    return new Date(deadline) < new Date();
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'AwaitingResponse': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'UnderReview': return 'bg-purple-400/10 text-purple-400 border-purple-400/20';
      case 'Resolved': return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      default: return 'bg-zinc-700 text-zinc-400 border-zinc-600';
    }
  }

  getVerdictClass(verdict?: string): string {
    switch (verdict) {
      case 'OwnerFavored': return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'BorrowerFavored': return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'Inconclusive': return 'bg-zinc-700 text-zinc-400 border-zinc-600';
      default: return 'bg-zinc-700 text-zinc-400 border-zinc-600';
    }
  }

}
