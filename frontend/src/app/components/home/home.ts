import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth-service';
import { ItemDTO } from '../../dtos/itemDTO';
import { UserService } from '../../services/user-service';

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit, AfterViewInit {

  private readonly base = 'https://localhost:7183';

  @ViewChild('categoryStrip') categoryStrip!: ElementRef;

 
  ngAfterViewInit() {
  const el = this.categoryStrip.nativeElement;

  const updateArrows = () => {
    this.showLeftArrow = el.scrollLeft > 0;
    this.showRightArrow = el.scrollLeft + el.clientWidth < el.scrollWidth - 1;
    this.cdr.detectChanges();
  };

  setTimeout(() => updateArrows(), 100);
  el.addEventListener('scroll', updateArrows);
  window.addEventListener('resize', updateArrows);

  // Drag to scroll
  let isDown = false;
  let startX = 0;
  let scrollLeft = 0;
  let hasDragged = false;

  el.addEventListener('mousedown', (e: MouseEvent) => {
    isDown = true;
    hasDragged = false;
    startX = e.pageX;
    scrollLeft = el.scrollLeft;
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

    // Only start dragging after moving 5px — prevents killing button clicks
    if (Math.abs(diff) > 5) {
      hasDragged = true;
      el.style.cursor = 'grabbing';
      el.scrollLeft = scrollLeft - diff;
    }
  });

  // Block click on child buttons if we dragged
  el.addEventListener('click', (e: MouseEvent) => {
    if (hasDragged) {
      e.stopPropagation();
      hasDragged = false;
    }
  }, true);
}



scrollCategories(dir: 'left' | 'right') {
  this.categoryStrip.nativeElement.scrollBy({ left: dir === 'right' ? 220 : -220, behavior: 'smooth' });
}

  // User info
  userName = '';
  userEmail = '';
  userInitials = '';
  userScore = 0;
  unreadCount = 0;
  userAvatarUrl: string | null = null;

  // UI state
  showUserMenu = false;
  isLoading = true;

  // Items
  allItems: ItemDTO.ItemSummaryDTO[] = [];
  filteredItems: ItemDTO.ItemSummaryDTO[] = [];

  // Filters
  searchQuery = '';
  selectedCategory: string | null = null;
  sortBy = 'newest';
  availableOnly = false;

  showLeftArrow = false;
  showRightArrow = false;

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

    electronics: '📱',
    tools: '🔧',
    sports: '⚽',
    music: '🎸',
    books: '📚',
    camping: '⛺',
    photography: '📷',
    cameras: '📷',
    gaming: '🎮',
    gardening: '🌱',
    garden: '🪴',
    biking: '🚲',
    bikes: '🚲',
    kitchen: '🍳',
    cleaning: '🧹',
    fashion: '👗',
    art: '🎨',
    baby: '👶',
    events: '🎉',
    auto: '🚗',
    other: '📦',
    others: '📦',
    // outdoors: '🏕️',
    // travel: '🧳',
    // fitness: '🏋️',
    // tech: '💻'
  };

  constructor(
    private authService: AuthService,
    private router: Router,
    private http: HttpClient,
    private cdr: ChangeDetectorRef,
    private userService: UserService
  ) { }

  ngOnInit() {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    this.loadUserInfo();
    this.loadItems();
  }

  private loadUserInfo() {


    this.userService.getMe().subscribe({
      next: (user) => {
        this.userName = user.fullName || user.username;
        this.userEmail = user.email;
        this.userInitials = this.getInitials(this.userName);
        this.userScore = user.score;
        this.userAvatarUrl = user.avatarUrl ?? null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load user info:', err);
      }
    });
  }

  private loadItems() {
    this.isLoading = true;
    this.http.get<ItemDTO.ItemSummaryDTO[]>(`${this.base}/api/items`).subscribe({
      next: (items) => {
        this.allItems = items;
        this.applyFilters();  //populate filteredItems first
        this.isLoading = false;
        this.cdr.detectChanges();

      },
      error: (err) => {
        console.error('Failed to load items:', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  onSearch() {
    this.applyFilters();
  }

  selectCategory(name: string | null) {
    this.selectedCategory = name;
    this.applyFilters();
  }

  applyFilters() {
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

    if (this.sortBy === 'rating') {
      result.sort((a, b) => b.averageRating - a.averageRating);
    } else if (this.sortBy === 'az') {
      result.sort((a, b) => a.title.localeCompare(b.title));
    } else {
      // newest — sort by id descending instead of reverse()
      result.sort((a, b) => b.id - a.id);  // ← fix: was result.reverse()
    }

    this.filteredItems = [...result]; // ← new array reference forces Angular to re-render
  }

  goToItem(id: number) {
    this.router.navigate(['/items', id]);
  }

  logout() {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/']),
      error: () => {
        //Clear tokens and redirect even if API call fails
        this.authService.clearTokens();
        this.router.navigate(['/']);
      },
    });
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getCategoryEmoji(categoryName: string): string {
    return this.emojiMap[categoryName.toLowerCase()] ?? '📦';
  }

  getConditionClass(condition: string): string {
    const c = condition?.toLowerCase();
    switch (c) {
      case 'excellent':
        return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'good':
        return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'fair':
        return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default:
        return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

}