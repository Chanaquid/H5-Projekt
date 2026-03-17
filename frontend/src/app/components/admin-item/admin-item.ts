import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { ItemService } from '../../services/item-service';
import { ItemDTO } from '../../dtos/itemDTO';
import { Navbar } from '../navbar/navbar';
import { CategoryService } from '../../services/category-service';
import { CategoryDTO } from '../../dtos/categoryDTO';
import { AdminService } from '../../services/admin-service';
import { AdminDTO } from '../../dtos/adminDTO';

@Component({
  selector: 'app-admin-item',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './admin-item.html',
  styleUrl: './admin-item.css',
})
export class AdminItem implements OnInit {
  allItems: ItemDTO.ItemSummaryDTO[] = [];
  filteredItems: ItemDTO.ItemSummaryDTO[] = [];
  isLoading = true;
  searchQuery = '';
  activeTab: 'approved' | 'pending' | 'rejected' = 'approved';

  itemHistory: AdminDTO.ItemHistoryDTO | null = null;
  visibleReviews = 5;
  visibleLoans = 5;

  // Modal
  showItemModal = false;
  selectedItem: ItemDTO.ItemSummaryDTO | null = null;
  itemDetail: ItemDTO.ItemDetailDTO | null = null;
  adminNote = '';
  isActioning = false;
  actionError = '';
  actionSuccess = '';
  selectedPhoto: string | null = null;

  // Addproperties
  showEditForm = false;
  categories: CategoryDTO.CategoryResponseDTO[] = [];
  editForm: ItemDTO.UpdateItemDTO | null = null;
  isUpdating = false;
  updateError = '';
  updateSuccess = '';

  tabs = [
    { key: 'approved' as const, label: 'Approved' },
    { key: 'pending' as const, label: 'Pending' },
    { key: 'rejected' as const, label: 'Rejected' },
  ];

  private emojiMap: Record<string, string> = {
    electronics: '📱', tools: '🔧', sports: '⚽', music: '🎸',
    books: '📚', camping: '⛺', photography: '📷', gaming: '🎮',
    gardening: '🌱', biking: '🚲', kitchen: '🍳', cleaning: '🧹',
    fashion: '👗', art: '🎨', baby: '👶', events: '🎉', auto: '🚗', other: '📦',
  };

  constructor(
    private authService: AuthService,
    private itemService: ItemService,
    private categoryService: CategoryService,
    private adminService: AdminService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }
    this.loadItems();
  }

  private loadItems(): void {
    this.isLoading = true;
    this.itemService.getAllAdmin(true).subscribe({
      next: (items) => {
        this.allItems = items;
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

  applyFilters(): void {
    let result = [...this.allItems];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(i =>
        i.title.toLowerCase().includes(q) ||
        i.ownerName.toLowerCase().includes(q) ||
        i.categoryName.toLowerCase().includes(q)
      );
    }

    switch (this.activeTab) {
      case 'approved': result = result.filter(i => i.status !== 'Pending' && i.status !== 'Rejected'); break;
      case 'pending': result = result.filter(i => i.status === 'Pending'); break;
      case 'rejected': result = result.filter(i => i.status === 'Rejected'); break;
    }

    this.filteredItems = result;
    this.cdr.detectChanges();
  }

  getTabCount(key: string): number {
    switch (key) {
      case 'approved': return this.allItems.filter(i => i.status !== 'Pending' && i.status !== 'Rejected').length;
      case 'pending': return this.allItems.filter(i => i.status === 'Pending').length;
      case 'rejected': return this.allItems.filter(i => i.status === 'Rejected').length;
      default: return 0;
    }
  }

  get approvedCount() { return this.allItems.filter(i => i.status !== 'Pending' && i.status !== 'Rejected').length; }
  get pendingCount() { return this.allItems.filter(i => i.status === 'Pending').length; }
  get rejectedCount() { return this.allItems.filter(i => i.status === 'Rejected').length; }

  openItemModal(item: ItemDTO.ItemSummaryDTO): void {
    this.selectedItem = item;
    this.itemDetail = null;
    this.itemHistory = null;
    this.adminNote = '';
    this.actionError = '';
    this.actionSuccess = '';
    this.showEditForm = false;
    this.showItemModal = true;
    this.visibleReviews = 5;
    this.visibleLoans = 5;

    this.itemService.getById(item.id).subscribe({
      next: (detail) => {
        this.itemDetail = detail;
        this.adminNote = detail.adminNote ?? '';
        this.cdr.detectChanges();
      }
    });


    this.adminService.getItemHistory(item.id).subscribe({
      next: (history) => {
        history.loans.sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime());
        this.itemHistory = history;
        this.cdr.detectChanges();
      }
    });
  }

  changeStatus(status: 'Approved' | 'Pending' | 'Rejected'): void {
    if (!this.selectedItem) return;
    if (status === 'Rejected' && !this.adminNote.trim()) {
      this.actionError = 'A note is required when rejecting an item.';
      return;
    }

    this.isActioning = true;
    this.actionError = '';
    this.actionSuccess = '';

    this.itemService.updateStatus(this.selectedItem.id, {
      status,
      adminNote: this.adminNote.trim() || undefined
    }).subscribe({
      next: (updated) => {
        this.itemDetail = updated;
        this.selectedItem!.status = updated.status;
        this.actionSuccess = `Status updated to ${status}.`;
        this.isActioning = false;
        this.applyFilters();
        this.cdr.detectChanges();
        setTimeout(() => { this.actionSuccess = ''; this.cdr.detectChanges(); }, 3000);
      },
      error: (err) => {
        this.actionError = err.error?.message ?? 'Failed to update status.';
        this.isActioning = false;
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

  openEditForm(): void {
    if (!this.itemDetail) return;
    this.editForm = {
      categoryId: this.itemDetail.category?.id,
      title: this.itemDetail.title,
      description: this.itemDetail.description,
      currentValue: this.itemDetail.currentValue,
      condition: this.itemDetail.condition as any,
      minLoanDays: this.itemDetail.minLoanDays,
      requiresVerification: this.itemDetail.requiresVerification,
      pickupAddress: this.itemDetail.pickupAddress,
      pickupLatitude: this.itemDetail.pickupLatitude,
      pickupLongitude: this.itemDetail.pickupLongitude,
      availableFrom: this.itemDetail.availableFrom?.split('T')[0],
      availableUntil: this.itemDetail.availableUntil?.split('T')[0],
      isActive: this.itemDetail.isActive,
    };
    this.showEditForm = true;
    this.updateError = '';
    this.updateSuccess = '';

    if (this.categories.length === 0) {
      this.categoryService.getAll().subscribe(cats => {
        this.categories = cats;
        this.cdr.detectChanges();
      });
    }
  }

  updateItem(): void {
    if (!this.selectedItem || !this.editForm) return;
    this.isUpdating = true;
    this.updateError = '';
    this.updateSuccess = '';

    this.itemService.update(this.selectedItem.id, this.editForm).subscribe({
      next: (updated) => {
        this.itemDetail = updated;
        this.selectedItem!.title = updated.title;
        this.selectedItem!.status = updated.status;
        this.selectedItem!.isActive = updated.isActive;
        this.isUpdating = false;
        this.updateSuccess = 'Item updated successfully.';
        this.showEditForm = false;
        this.applyFilters();
        this.cdr.detectChanges();
        setTimeout(() => { this.updateSuccess = ''; this.cdr.detectChanges(); }, 3000);
      },
      error: (err) => {
        this.updateError = err.error?.message ?? 'Failed to update item.';
        this.isUpdating = false;
        this.cdr.detectChanges();
      }
    });
  }

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active': return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'approved': return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'returned': return 'bg-zinc-300/10 text-zinc-300 border-zinc-300/20';
      case 'late': return 'bg-red-400/10 text-red-400 border-red-400/20';
      case 'pending': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'cancelled':
      case 'rejected': return 'bg-zinc-700 text-zinc-400 border-zinc-600';
      default: return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  get displayedReviews() {
    return this.itemHistory?.reviews.slice(0, this.visibleReviews) ?? [];
  }

  get displayedLoans() {
    return this.itemHistory?.loans.slice(0, this.visibleLoans) ?? [];
  }

  loadMoreReviews(): void {
    this.visibleReviews += 5;
  }

  loadMoreLoans(): void {
    this.visibleLoans += 5;
  }


}
