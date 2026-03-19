import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { UserService } from '../../services/user-service';
import { ItemService } from '../../services/item-service';
import { ReviewService } from '../../services/review-service';
import { UserDTO } from '../../dtos/userDTO';
import { ItemDTO } from '../../dtos/itemDTO';
import { ReviewDTO } from '../../dtos/reviewDTO';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-user-profile',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './user-profile.html',
  styleUrl: './user-profile.css',
})
export class UserProfile implements OnInit {

  userId = '';
  profile: UserDTO.UserSummaryDTO | null = null;
  items: ItemDTO.ItemSummaryDTO[] = [];
  filteredItems: ItemDTO.ItemSummaryDTO[] = [];
  reviews: ReviewDTO.UserReviewResponseDTO[] = [];
  displayedReviews: ReviewDTO.UserReviewResponseDTO[] = [];

  itemsCollapsed = false;

  isLoadingProfile = true;
  isLoadingItems = true;
  isLoadingReviews = true;

  // Item filters
  searchQuery = '';
  activeFilter: 'all' | 'available' | 'onloan' = 'all';
  visibleReviews = 5;

  get averageRating(): number {
    if (!this.reviews.length) return 0;
    return Math.round((this.reviews.reduce((s, r) => s + r.rating, 0) / this.reviews.length) * 10) / 10;
  }

  get scoreColor(): string {
    const s = this.profile?.score ?? 0;
    if (s >= 70) return 'text-emerald-400';
    if (s >= 40) return 'text-amber-400';
    return 'text-red-400';
  }

  get scoreBg(): string {
    const s = this.profile?.score ?? 0;
    if (s >= 70) return 'bg-emerald-400/10 border-emerald-400/20';
    if (s >= 40) return 'bg-amber-400/10 border-amber-400/20';
    return 'bg-red-400/10 border-red-400/20';
  }

  private emojiMap: Record<string, string> = {
    electronics: '📱', tools: '🔧', sports: '⚽', music: '🎸',
    books: '📚', camping: '⛺', photography: '📷', gaming: '🎮',
    gardening: '🌱', biking: '🚲', kitchen: '🍳', cleaning: '🧹',
    fashion: '👗', art: '🎨', baby: '👶', events: '🎉', auto: '🚗', other: '📦',
  };

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private itemService: ItemService,
    private reviewService: ReviewService,
    private route: ActivatedRoute,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    this.route.params.subscribe(params => {
      this.userId = params['id'];
      this.load();
    });
  }

  private load(): void {
    this.isLoadingProfile = true;
    this.isLoadingItems = true;
    this.isLoadingReviews = true;
    this.profile = null;
    this.items = [];
    this.reviews = [];
    this.visibleReviews = 5;

    this.userService.getPublicProfile(this.userId).subscribe({
      next: (p) => {
        this.profile = p;
        this.isLoadingProfile = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingProfile = false;
        this.cdr.detectChanges();
      }
    });

    this.itemService.getByUserPublic(this.userId).subscribe({
      next: (items) => {
        this.items = items;
        this.applyFilters();
        this.isLoadingItems = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingItems = false;
        this.cdr.detectChanges();
      }
    });

    this.reviewService.getUserReviews(this.userId).subscribe({
      next: (reviews) => {
        this.reviews = reviews;
        this.updateDisplayedReviews();
        this.isLoadingReviews = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingReviews = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters(): void {
    let result = [...this.items];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(i =>
        i.title.toLowerCase().includes(q) ||
        i.categoryName.toLowerCase().includes(q)
      );
    }

    switch (this.activeFilter) {
      case 'available': result = result.filter(i => !i.isCurrentlyOnLoan && i.isActive); break;
      case 'onloan':    result = result.filter(i => i.isCurrentlyOnLoan); break;
    }

    this.filteredItems = result;
    this.cdr.detectChanges();
  }

  updateDisplayedReviews(): void {
    this.displayedReviews = this.reviews.slice(0, this.visibleReviews);
  }

  loadMoreReviews(): void {
    this.visibleReviews += 5;
    this.updateDisplayedReviews();
  }

  getCategoryEmoji(cat: string): string {
    return this.emojiMap[cat?.toLowerCase()] ?? '📦';
  }

  getConditionClass(condition: string): string {
    switch (condition?.toLowerCase()) {
      case 'excellent': return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'good':      return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case 'fair':      return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'poor':      return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default:          return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }


}
