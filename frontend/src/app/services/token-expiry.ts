import { Injectable, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { AuthService } from './auth-service';

@Injectable({
  providedIn: 'root',
})
export class TokenExpiry implements OnDestroy {

  readonly sessionWarning$ = new Subject<number>();

  readonly sessionExpired$ = new Subject<void>();
 
  private warningTimer: ReturnType<typeof setTimeout> | null = null;
  private expireTimer:  ReturnType<typeof setTimeout> | null = null;
 
  constructor(private authService: AuthService) {}

  scheduleWarning(): void {
    this.clear();
    const token = this.authService.getToken();
    if (!token) return;

    const secondsLeft = this.getSecondsUntilExpiry(token);
    if (secondsLeft <= 0) {
      this.authService.clearTokens();
      sessionStorage.removeItem('session_warning');
      this.sessionExpired$.next();
      return;
    }

    const WARN_BEFORE_MS = 1 * 60 * 1000;
    const expiryMs = secondsLeft * 1000;
    const warnInMs = expiryMs - WARN_BEFORE_MS;

    if (warnInMs > 0) {
      this.warningTimer = setTimeout(() => {
        const remaining = this.getSecondsUntilExpiry(this.authService.getToken() ?? '');
        sessionStorage.setItem('session_warning', 'true'); // ← persist
        this.sessionWarning$.next(remaining);
      }, warnInMs);
    } else {
      sessionStorage.setItem('session_warning', 'true'); // ← persist immediately
      this.sessionWarning$.next(secondsLeft);
    }

    this.expireTimer = setTimeout(() => {
      this.authService.clearTokens();
      sessionStorage.removeItem('session_warning');
      this.sessionExpired$.next();
    }, expiryMs);
  }
 
  clear(): void {
    if (this.warningTimer !== null) { clearTimeout(this.warningTimer); this.warningTimer = null; }
    if (this.expireTimer  !== null) { clearTimeout(this.expireTimer);  this.expireTimer  = null; }
    sessionStorage.removeItem('session_warning');
  }
 
  //Decodes the JWT and returns seconds until expiry (may be negative).
  public getSecondsUntilExpiry(token: string): number {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp: number = payload['exp'];
      return exp - Math.floor(Date.now() / 1000);
    } catch {
      return 0;
    }
  }
 
  ngOnDestroy(): void { this.clear(); }
}
