import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth-service';
import { ItemDTO } from '../../dtos/itemDTO';
import { UserService } from '../../services/user-service';
import { FavoriteService } from '../../services/favorite-service';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit, AfterViewInit {

  private readonly base = 'https://localhost:7183';

  @ViewChild('categoryStrip') categoryStrip!: ElementRef;

  userName = '';
  isLoading = true;

  allItems: ItemDTO.ItemSummaryDTO[] = [];
  filteredItems: ItemDTO.ItemSummaryDTO[] = [];

  searchQuery = '';
  selectedCategory: string | null = null;
  sortBy = 'newest';
  availableOnly = false;

  showLeftArrow = false;
  showRightArrow = false;

  // Favorites
  favoriteIds = new Set<number>();
  togglingIds = new Set<number>();

  //Toast error emssage
  toastMessage = '';
  toastVisible = false;
  private toastTimeout: any;

  //Pagination
  currentPage = 1;
  pageSize = 20;

  currentUserId = '';


  categories = [
    { icon: '📱', name: 'Electronics' },
    { icon: '🔧', name: 'Tools' },
    { icon: '⚽', name: 'Sports' },
    { icon: '🎸', name: 'Music' },
    { icon: '📚', name: 'Books' },
    { icon: '⛺', name: 'Camping' },
    { icon: '📷', name: 'Photography' },
    { icon: '🎮', name: 'Gaming' },
    { icon: '🌱', name: 'Gardening' },
    { icon: '🚲', name: 'Biking' },
    { icon: '🍳', name: 'Kitchen' },
    { icon: '🧹', name: 'Cleaning' },
    { icon: '👗', name: 'Fashion' },
    { icon: '🎨', name: 'Art' },
    { icon: '👶', name: 'Baby' },
    { icon: '🎉', name: 'Events' },
    { icon: '🚗', name: 'Auto' },
    { icon: '📦', name: 'Other' },
  ];

  private emojiMap: Record<string, string> = {
    electronics: '📱', tools: '🔧', sports: '⚽', music: '🎸',
    books: '📚', camping: '⛺', photography: '📷', cameras: '📷',
    gaming: '🎮', gardening: '🌱', garden: '🪴', biking: '🚲',
    bikes: '🚲', kitchen: '🍳', cleaning: '🧹', fashion: '👗',
    art: '🎨', baby: '👶', events: '🎉', auto: '🚗',
    other: '📦', others: '📦',
  };

  constructor(
    private authService: AuthService,
    private router: Router,
    private http: HttpClient,
    private cdr: ChangeDetectorRef,
    private userService: UserService,
    private favoriteService: FavoriteService,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    this.loadUserInfo();
    this.loadItems();
    this.loadFavorites();

    this.route.queryParams.subscribe(params => {
      this.searchQuery = params['q'] || '';
      this.applyFilters();
    });
  }

  ngAfterViewInit(): void {
    const el = this.categoryStrip.nativeElement;

    const updateArrows = () => {
      this.showLeftArrow = el.scrollLeft > 0;
      this.showRightArrow = el.scrollLeft + el.clientWidth < el.scrollWidth - 1;
      this.cdr.detectChanges();
    };

    setTimeout(() => updateArrows(), 100);
    el.addEventListener('scroll', updateArrows);
    window.addEventListener('resize', updateArrows);

    let isDown = false, startX = 0, scrollLeft = 0, hasDragged = false;

    el.addEventListener('mousedown', (e: MouseEvent) => {
      isDown = true; hasDragged = false;
      startX = e.pageX; scrollLeft = el.scrollLeft;
      el.style.userSelect = 'none';
    });

    window.addEventListener('mouseup', () => {
      isDown = false;
      el.style.cursor = 'grab';
      el.style.userSelect = '';
    });

    window.addEventListener('mousemove', (e: MouseEvent) => {
      if (!isDown) return;
      const diff = e.pageX - startX;
      if (Math.abs(diff) > 5) {
        hasDragged = true;
        el.style.cursor = 'grabbing';
        el.scrollLeft = scrollLeft - diff;
      }
    });

    el.addEventListener('click', (e: MouseEvent) => {
      if (hasDragged) { e.stopPropagation(); hasDragged = false; }
    }, true);
  }



  private loadUserInfo(): void {
    this.userService.getMe().subscribe({
      next: (user) => {
        this.userName = user.fullName || user.username;
        this.currentUserId = user.id;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load user info:', err)
    });
  }

  private loadItems(): void {
    this.isLoading = true;
    this.http.get<ItemDTO.ItemSummaryDTO[]>(`${this.base}/api/items`).subscribe({
      next: (items) => {
        this.allItems = items;
        this.applyFilters();
        this.isLoading = false;
        console.log('Loaded items:', this.allItems);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load items:', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }


  private loadFavorites(): void {
    if (!this.authService.isLoggedIn()) return;
    this.favoriteService.getMyFavorites().subscribe({
      next: (favs) => {
        this.favoriteIds = new Set(favs.map(f => f.item.id));
        this.cdr.detectChanges();
      },
      error: () => { }
    });
  }


  toggleFavorite(itemId: number, event: Event): void {
    event.stopPropagation();
    if (this.togglingIds.has(itemId)) return;
    this.togglingIds.add(itemId);

    const isFav = this.favoriteIds.has(itemId);
    const action = isFav
      ? this.favoriteService.removeFavorite(itemId)
      : this.favoriteService.addFavorite(itemId);

    action.subscribe({
      next: () => {
        if (isFav) this.favoriteIds.delete(itemId);
        else this.favoriteIds.add(itemId);
        this.togglingIds.delete(itemId);
        this.cdr.detectChanges();
      },
      error: (err) => {
        if (err.status === 409) {
          this.favoriteIds.add(itemId);
          this.showToast('Already in your favorites.');
        } else if (err.status === 404) {
          this.favoriteIds.delete(itemId);
          this.showToast('Item not found in favorites.');
        } else {
          this.showToast(err.error?.message ?? 'Something went wrong.');
        }
        this.togglingIds.delete(itemId);
        this.cdr.detectChanges();
      }
    });
  }


  onSearch(): void { this.applyFilters(); }

  selectCategory(name: string | null): void {
    this.selectedCategory = name;
    this.applyFilters();
  }

  applyFilters(): void {
    this.currentPage = 1;

    let result = [...this.allItems];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(i =>
        i.title.toLowerCase().includes(q) ||
        i.categoryName.toLowerCase().includes(q) ||
        i.pickupAddress.toLowerCase().includes(q) ||
        i.ownerName.toLowerCase().includes(q)
      );
    }

    if (this.selectedCategory) {
      result = result.filter(i =>
        i.categoryName.toLowerCase() === this.selectedCategory!.toLowerCase()
      );
    }

    if (this.availableOnly) {
      result = result.filter(i => !i.isCurrentlyOnLoan);
    }

    switch (this.sortBy) {
      case 'oldest': result.sort((a, b) => a.id - b.id); break;
      case 'rating': result.sort((a, b) => b.averageRating - a.averageRating); break;
      case 'az': result.sort((a, b) => a.title.localeCompare(b.title)); break;
      case 'za': result.sort((a, b) => b.title.localeCompare(a.title)); break;
      default: result.sort((a, b) => b.id - a.id); break;
    }

    this.filteredItems = [...result];
  }

  scrollCategories(dir: 'left' | 'right'): void {
    this.categoryStrip.nativeElement.scrollBy({
      left: dir === 'right' ? 220 : -220,
      behavior: 'smooth'
    });
  }

  goToItem(id: number): void { this.router.navigate(['/items', id]); }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getCategoryEmoji(categoryName: string): string {
    return this.emojiMap[categoryName.toLowerCase()] ?? '📦';
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

  showToast(message: string): void {
    this.toastMessage = message;
    this.toastVisible = true;
    this.cdr.detectChanges();

    clearTimeout(this.toastTimeout);
    this.toastTimeout = setTimeout(() => {
      this.toastVisible = false;
      this.cdr.detectChanges();
    }, 3000);
  }

  get totalPages(): number {
    return Math.ceil(this.filteredItems.length / this.pageSize);
  }

  get paginatedItems(): ItemDTO.ItemSummaryDTO[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredItems.slice(start, start + this.pageSize);
  }

  get pageNumbers(): number[] {
    const total = this.totalPages;
    const current = this.currentPage;
    const pages: number[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push(-1); // ellipsis
      for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
        pages.push(i);
      }
      if (current < total - 2) pages.push(-1); // ellipsis
      pages.push(total);
    }
    return pages;
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.cdr.detectChanges();
  }


}