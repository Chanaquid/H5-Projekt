import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth-service';
import { FavoriteService } from '../../services/favorite-service';
import { FavoriteDTO } from '../../dtos/favoriteDTO';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-favorites',
  imports: [CommonModule, RouterLink, Navbar],
  templateUrl: './favorites.html',
  styleUrl: './favorites.css',
})
export class Favorites implements OnInit {

  favorites: FavoriteDTO.FavoriteResponseDTO[] = [];
  isLoading = true;
  removingIds = new Set<number>();

  private emojiMap: Record<string, string> = {
    electronics: '📱', tools: '🔧', sports: '⚽', music: '🎸',
    books: '📚', camping: '⛺', photography: '📷', gaming: '🎮',
    gardening: '🌱', biking: '🚲', kitchen: '🍳', cleaning: '🧹',
    fashion: '👗', art: '🎨', baby: '👶', events: '🎉', auto: '🚗', other: '📦',
  };

  constructor(
    private authService: AuthService,
    private favoriteService: FavoriteService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    this.loadFavorites();
  }

  private loadFavorites(): void {
    this.isLoading = true;
    this.favoriteService.getMyFavorites().subscribe({
      next: (favs) => {
        this.favorites = favs;
        this.isLoading = false;
        this.cdr.detectChanges();
        console.log(this.favorites)
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  removeFavorite(itemId: number, event: Event): void {
    event.stopPropagation();
    this.removingIds.add(itemId);
    this.favoriteService.removeFavorite(itemId).subscribe({
      next: () => {
        this.favorites = this.favorites.filter(f => f.item.id !== itemId);
        this.removingIds.delete(itemId);
        this.cdr.detectChanges();
      },
      error: () => {
        this.removingIds.delete(itemId);
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
      case 'good':      return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case 'fair':      return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'poor':      return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default:          return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }
}