import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
 
import { AuthService } from '../../services/auth-service';
import { TokenExpiry } from '../../services/token-expiry';


@Component({
  selector: 'app-session-toast',
  imports: [],
  templateUrl: './session-toast.html',
  styleUrl: './session-toast.css',
})
export class SessionToast implements OnInit, OnDestroy {

  visible   = false;
  extending = false;


  private initialSeconds = 300;
  private secondsLeft    = 300;


  private tickInterval: ReturnType<typeof setInterval> | null = null;
  private subs = new Subscription();

  constructor(
    private authService:       AuthService,
    private tokenExpiryService: TokenExpiry,
    private router:            Router,
    private cdr:               ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    // Show toast when the warning fires
    this.subs.add(
      this.tokenExpiryService.sessionWarning$.subscribe((secondsLeft) => {
        this.initialSeconds = secondsLeft;
        this.secondsLeft    = secondsLeft;
        this.visible        = true;
        this.startCountdown();
        this.cdr.markForCheck();
      })
    );
 
    // If the token fully expires while the toast is open, force sign-out
    this.subs.add(
      this.tokenExpiryService.sessionExpired$.subscribe(() => {
        this.stopCountdown();
        this.visible = false;
        this.cdr.markForCheck();
        this.router.navigate(['/']);
      })
    );
  }


  get countdownLabel(): string {
    const s = Math.max(0, this.secondsLeft);
    if (s >= 60) {
      const m = Math.floor(s / 60);
      const sec = s % 60;
      return `${m}:${sec.toString().padStart(2, '0')}`;
    }
    return `${s}s`;
  }
 
  get progressPct(): number {
    if (this.initialSeconds <= 0) return 0;
    return Math.max(0, (this.secondsLeft / this.initialSeconds) * 100);
  }


  extend(): void {
      if (this.extending) return;
      this.extending = true;
      this.cdr.markForCheck();
  
      const refreshToken = this.authService.getRefreshToken();
      if (!refreshToken) {
        this.forceSignOut();
        return;
      }
  
      this.authService.refresh({ refreshToken }).subscribe({
        next: () => {
          // New token saved by AuthService.saveTokens via the tap() pipe.
          // Re-schedule the expiry timers for the new token.
          this.tokenExpiryService.scheduleWarning();
  
          this.extending = false;
          this.visible   = false;
          this.stopCountdown();
          this.cdr.markForCheck();
        },
        error: () => {
          // Refresh failed — clear everything and redirect
          this.forceSignOut();
        },
      });
  }
  

  dismiss(): void {
    this.stopCountdown();
    this.visible = false;
    this.cdr.markForCheck();
    this.authService.logout().subscribe({
      error: () => { /* swallow — tokens already cleared by AuthService */ }
    });
    this.authService.clearTokens();
    this.tokenExpiryService.clear();
    this.router.navigate(['/']);
  }
 
  private startCountdown(): void {
    this.stopCountdown();
    this.tickInterval = setInterval(() => {
      this.secondsLeft = Math.max(0, this.secondsLeft - 1);
      this.cdr.markForCheck();
      if (this.secondsLeft <= 0) {
        this.stopCountdown();
      }
    }, 1000);
  }
 
  private stopCountdown(): void {
    if (this.tickInterval !== null) {
      clearInterval(this.tickInterval);
      this.tickInterval = null;
    }
  }

  private forceSignOut(): void {
    this.authService.clearTokens();
    this.tokenExpiryService.clear();
    this.stopCountdown();
    this.visible   = false;
    this.extending = false;
    this.cdr.markForCheck();
    this.router.navigate(['/']);
  }
 
  ngOnDestroy(): void {
    this.stopCountdown();
    this.subs.unsubscribe();
  }




}
