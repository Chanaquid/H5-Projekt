import { Component, OnInit, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { SessionToast } from "./components/session-toast/session-toast";
import { AuthService } from './services/auth-service';
import { TokenExpiry } from './services/token-expiry';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SessionToast],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('frontend');

  constructor(
    private authService: AuthService,
    private tokenExpiryService: TokenExpiry,
    private router: Router,
  ) {}


  ngOnInit(): void {
    //Wire the scheduler so AuthService can re-schedule after token refresh
    this.authService.setScheduler(() => this.tokenExpiryService.scheduleWarning());

    //If already logged in on app load (e.g. page refresh), start the timers
    if (this.authService.isLoggedIn()) {
      setTimeout(() => {
        this.tokenExpiryService.scheduleWarning();

        // If the warning was already showing before the refresh, re-emit immediately
        if (sessionStorage.getItem('session_warning')) {
          const token = this.authService.getToken()!;
          const secondsLeft = this.tokenExpiryService.getSecondsUntilExpiry(token);
          if (secondsLeft > 0) {
            this.tokenExpiryService.sessionWarning$.next(secondsLeft);
          }
        }
      }, 0);
    }

    //Redirect on hard expiry from anywhere in the app
    this.tokenExpiryService.sessionExpired$.subscribe(() => {
      this.router.navigate(['/']);
    });
  }




}
