import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { UserService } from '../../services/user-service';
import { NotificationDTO } from '../../dtos/notificationDTO';
import { NotificationService } from '../../services/notification-service';
import { filter } from 'rxjs/operators';


@Component({
  selector: 'app-navbar',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})



export class Navbar implements OnInit {

  userName = '';
  userEmail = '';
  userInitials = '';
  userAvatarUrl: string | null = null;

  showUserMenu = false;
  showNotifications = false;

  notifications: NotificationDTO.NotificationResponseDTO[] = [];
  unreadCount = 0;

  searchQuery = '';
  isHomePage = false;

isAdmin = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private userService: UserService,
    private notificationService: NotificationService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadSummary();
    this.isAdmin = this.authService.isAdmin();


    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe((e: NavigationEnd) => {
      this.isHomePage = e.urlAfterRedirects.startsWith('/home');
      this.cdr.detectChanges();
    });

    // Set initial value on first load
    this.isHomePage = this.router.url.startsWith('/home');
  }

  private loadUserInfo(): void {
    this.userService.getMe().subscribe({
      next: (user) => {
        this.userName = user.fullName || user.username;
        this.userEmail = user.email;
        this.userInitials = this.getInitials(this.userName);
        this.userAvatarUrl = user.avatarUrl ?? null;
        this.cdr.detectChanges();

      },
      error: (err) => console.error('Failed to load user info:', err)
    });
  }

  loadSummary(): void {
    this.notificationService.getSummary().subscribe(res => {
      this.unreadCount = res.unreadCount;
      this.notifications = res.recent;
      this.cdr.detectChanges();
    });
  }

  onNotificationClick(n: NotificationDTO.NotificationResponseDTO): void {
    if (!n.isRead) {
      n.isRead = true; // update UI immediately
      this.unreadCount = Math.max(0, this.unreadCount - 1);
      this.notificationService.markAsRead(n.id).subscribe({
        error: () => {
          // revert if API fails
          n.isRead = false;
          this.unreadCount++;
        }
      });
    }
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe(() => {
      this.notifications.forEach(n => n.isRead = true);
      this.unreadCount = 0;
      this.cdr.detectChanges();
    });
  }

  onSearch(): void {
    this.router.navigate(['/home'], {
      queryParams: { q: this.searchQuery.trim() || null },
      queryParamsHandling: 'merge'
    });
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/']),
      error: () => {
        this.authService.clearTokens();
        this.router.navigate(['/']);
      }
    });
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getNotificationIcon(type: string): string {
    switch (type) {
      //Loan lifecycle
      case 'LoanRequested': return '📋';
      case 'LoanApproved': return '✅';
      case 'LoanRejected': return '❌';
      case 'LoanCancelled': return '🚫';
      case 'LoanActive': return '🤝';
      case 'LoanReturned': return '📦';
      //Due dates
      case 'DueSoon': return '⏰';
      case 'Overdue': return '🔴';
      //Item lifecycle
      case 'ItemApproved': return '✅';
      case 'ItemRejected': return '❌';
      case 'ItemAvailable': return '🟢';
      //Fines and score
      case 'FineIssued': return '⚠️';
      case 'FinePaid': return '💰';
      case 'ScoreChanged': return '📊';
      //Disputes
      case 'DisputeFiled': return '⚖️';
      case 'DisputeResponse': return '📝';
      case 'DisputeResolved': return '🏛️';
      //Appeals
      case 'AppealSubmitted': return '📤';
      case 'AppealApproved': return '✅';
      case 'AppealRejected': return '❌';
      //Verification
      case 'VerificationApproved': return '🏅';
      case 'VerificationRejected': return '❌';
      //Messages
      case 'MessageReceived': return '💬';
      case 'DirectMessageReceived': return '✉️';
      case 'SupportMessageReceived': return '🎧';
      default: return '🔔';
    }
  }

  getNotificationIconBg(type: string): string {
    switch (type) {
      //Green — positive outcomes
      case 'LoanApproved':
      case 'LoanActive':
      case 'ItemApproved':
      case 'ItemAvailable':
      case 'FinePaid':
      case 'AppealApproved':
      case 'VerificationApproved': return 'bg-emerald-400/10';
      //Red — negative outcomes
      case 'LoanRejected':
      case 'LoanCancelled':
      case 'ItemRejected':
      case 'FineIssued':
      case 'Overdue':
      case 'AppealRejected':
      case 'VerificationRejected':
        return 'bg-red-400/10';

      //Amber — pending / action needed
      case 'LoanRequested':
      case 'DueSoon':
      case 'DisputeFiled':
      case 'DisputeResponse':
      case 'AppealSubmitted':
        return 'bg-amber-400/10';

      //Blue — informational
      case 'LoanReturned':
      case 'ScoreChanged':
      case 'DisputeResolved':
      case 'MessageReceived':
      case 'DirectMessageReceived':
      case 'SupportMessageReceived':
        return 'bg-blue-400/10';

      default:
        return 'bg-zinc-800';
    }
  }


}