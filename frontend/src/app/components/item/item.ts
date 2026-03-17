import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { ItemService } from '../../services/item-service';
import { Navbar } from '../navbar/navbar';
import { CategoryService } from '../../services/category-service';
import { CategoryDTO } from '../../dtos/categoryDTO';
import { ItemDTO } from '../../dtos/itemDTO';
import { ItemCondition } from '../../dtos/enums';

@Component({
  selector: 'app-item',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './item.html',
  styleUrl: './item.css',
})
export class Item implements OnInit {

  //items
  allItems: ItemDTO.ItemSummaryDTO[] = [];
  filteredItems: ItemDTO.ItemSummaryDTO[] = [];

  isLoading = true;
  searchQuery = '';
  activeTab: 'all' | 'active' | 'pending' | 'rejected' | 'inactive' = 'all';

  showDeleteModal = false;
  itemToDelete: ItemDTO.ItemSummaryDTO | null = null;
  isDeleting = false;

  tabs = [
    { key: 'all' as const, label: 'All' },
    { key: 'active' as const, label: 'Active' },
    { key: 'pending' as const, label: 'Pending' },
    { key: 'rejected' as const, label: 'Rejected' },
    { key: 'inactive' as const, label: 'Inactive' },
  ];

  private emojiMap: Record<string, string> = {
    electronics: '📱', tools: '🔧', sports: '⚽', music: '🎸',
    books: '📚', camping: '⛺', photography: '📷', gaming: '🎮',
    gardening: '🌱', biking: '🚲', kitchen: '🍳', cleaning: '🧹',
    fashion: '👗', art: '🎨', baby: '👶', events: '🎉', auto: '🚗', other: '📦',
  };

  //add Items
  showAddModal = false;
  isCreating = false;
  createError = '';
  categories: CategoryDTO.CategoryResponseDTO[] = [];
  createForm: ItemDTO.CreateItemDTO & { photoUrl?: string } = this.emptyCreateForm();
  addressSuggestions: any[] = [];
  showAddressSuggestions = false;
  private addressSearchTimeout: any;
  isAdmin = false;

  emptyCreateForm(): ItemDTO.CreateItemDTO & { photoUrl?: string } {
    return {
      categoryId: 0,
      title: '',
      description: '',
      currentValue: 0,
      condition: ItemCondition.Good,
      minLoanDays: undefined,
      requiresVerification: false,
      pickupAddress: '',
      pickupLatitude: undefined,
      pickupLongitude: undefined,
      availableFrom: '',
      availableUntil: '',
      photoUrl: '',

    };
  }

  //For item pics
  createdItemId: number | null = null;
  wantsToAddPhotos = false;
  showPhotoModal = false;
  selectedFiles: File[] = [];
  isUploadingPhotos = false;
  photoUploadError = '';
  photoUploadSuccess = false;

  constructor(
    private authService: AuthService,
    private itemService: ItemService,
    private categoryService: CategoryService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    this.isAdmin = this.authService.isAdmin();
    this.loadItems();
  }

  private loadItems(): void {
    this.isLoading = true;
    this.itemService.getMyItems().subscribe({
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
        i.categoryName.toLowerCase().includes(q)
      );
    }

    switch (this.activeTab) {
      case 'active': result = result.filter(i => i.status === 'Approved' && i.isActive !== false); break;
      case 'pending': result = result.filter(i => i.status === 'Pending'); break;
      case 'rejected': result = result.filter(i => i.status === 'Rejected'); break;
      case 'inactive': result = result.filter(i => i.isActive === false); break;
    }

    this.filteredItems = result;
  }

  getTabCount(key: string): number {
    switch (key) {
      case 'all': return this.allItems.length;
      case 'active': return this.allItems.filter(i => i.status === 'Approved' && i.isActive !== false).length;
      case 'pending': return this.allItems.filter(i => i.status === 'Pending').length;
      case 'rejected': return this.allItems.filter(i => i.status === 'Rejected').length;
      case 'inactive': return this.allItems.filter(i => i.isActive === false).length;
      default: return 0;
    }
  }

  get approvedCount() { return this.allItems.filter(i => i.status === 'Approved' && i.isActive !== false).length; }
  get pendingCount() { return this.allItems.filter(i => i.status === 'Pending').length; }
  get rejectedCount() { return this.allItems.filter(i => i.status === 'Rejected').length; }
  get inactiveCount() { return this.allItems.filter(i => i.isActive === false).length; }

  toggleActive(item: ItemDTO.ItemSummaryDTO): void {
    this.itemService.toggleActive(item.id, !item.isActive).subscribe({
      next: () => {
        item.isActive = !item.isActive;
        this.applyFilters();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to toggle active:', err);
      }
    });
  }
  editItem(item: ItemDTO.ItemSummaryDTO): void {
    this.router.navigate(['/items', item.id, 'edit']);
  }

  goToItem(id: number): void {
    this.router.navigate(['/items', id]);
  }

  confirmDelete(item: ItemDTO.ItemSummaryDTO): void {
    this.itemToDelete = item;
    this.showDeleteModal = true;
  }

  deleteItem(): void {
    if (!this.itemToDelete) return;
    this.isDeleting = true;
    this.itemService.delete(this.itemToDelete.id).subscribe({
      next: () => {
        this.allItems = this.allItems.filter(i => i.id !== this.itemToDelete!.id);
        this.applyFilters();
        this.showDeleteModal = false;
        this.itemToDelete = null;
        this.isDeleting = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isDeleting = false;
        this.cdr.detectChanges();
      }
    });
  }

  getCategoryEmoji(cat: string): string {
    return this.emojiMap[cat.toLowerCase()] ?? '📦';
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

  openAddModal(): void {
    this.createForm = this.emptyCreateForm();
    this.createError = '';
    this.showAddModal = true;
    if (this.categories.length === 0) {
      this.categoryService.getAll().subscribe(cats => {
        this.categories = cats;
        this.cdr.detectChanges();
      });
    }
  }

  createItem(): void {
    if (!this.createForm.title || !this.createForm.categoryId || !this.createForm.availableFrom || !this.createForm.availableUntil) {
      this.createError = 'Please fill in all required fields.';
      return;
    }
    this.isCreating = true;
    this.createError = '';

    const { photoUrl, ...itemDto } = this.createForm;

    this.itemService.create(itemDto).subscribe({
      next: (created) => {
        this.createdItemId = created.id;

        if (photoUrl?.trim()) {
          this.itemService.addPhoto(created.id, {
            photoUrl: photoUrl.trim(),
            isPrimary: true,
            displayOrder: 0
          }).subscribe({
            next: () => {
              this.showAddModal = false;
              this.isCreating = false;
              this.cdr.detectChanges();
              this.loadItems();
            },
            error: () => {
              //Item was created, photo failed — still close and refresh
              this.showAddModal = false;
              this.isCreating = false;
              this.cdr.detectChanges();
              this.loadItems();
            }
          });
        } else {
          this.showAddModal = false;
          this.isCreating = false;
          this.cdr.detectChanges();
          this.loadItems();
        }
      },
      error: (err) => {
        this.createError = err.error?.message ?? 'Failed to create item.';
        this.isCreating = false;
        this.cdr.detectChanges();
      }
    });
  }


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

  selectAddress(place: any): void {
    const a = place.address;
    const parts = [
      [a?.road ?? '', a?.house_number ?? ''].filter(Boolean).join(' '),
      a?.neighbourhood ?? a?.suburb ?? '',
      a?.city ?? a?.town ?? a?.village ?? '',
    ].filter(Boolean);
    this.createForm.pickupAddress = parts.join(', ') || place.display_name;
    this.createForm.pickupLatitude = parseFloat(place.lat);
    this.createForm.pickupLongitude = parseFloat(place.lon);
    this.addressSuggestions = [];
    this.showAddressSuggestions = false;
    this.cdr.detectChanges();
  }


}




