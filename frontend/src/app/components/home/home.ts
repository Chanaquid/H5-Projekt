import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth-service';
import { ItemDTO } from '../../dtos/itemDTO';
import { UserService } from '../../services/user-service';
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
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    this.loadUserInfo();
    this.loadItems();
    
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
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load items:', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  onSearch(): void { this.applyFilters(); }

  selectCategory(name: string | null): void {
    this.selectedCategory = name;
    this.applyFilters();
  }

  applyFilters(): void {
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
      result.sort((a, b) => b.id - a.id);
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
    const c = condition?.toLowerCase();
    switch (c) {
      case 'excellent': return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'good':      return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case 'fair':      return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'poor':      return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default:          return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }
}