import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthDTO } from '../dtos/authDTO';
import { ApiResponseDTO } from '../dtos/apiResponseDTO';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly baseUrl = 'https://localhost:7183/api/auth';

    private _scheduleWarning?: () => void;
    setScheduler(fn: () => void): void { this._scheduleWarning = fn; }
 

  constructor(private http: HttpClient) {}

  //POST /api/auth/register
  register(dto: AuthDTO.RegisterDTO): Observable<AuthDTO.AuthResponseDTO> {
    return this.http
      .post<AuthDTO.AuthResponseDTO>(`${this.baseUrl}/register`, dto)
      .pipe(tap((res) => { this.saveTokens(res.token, res.refreshToken); this._scheduleWarning?.(); }));
  }

  //GET /api/auth/confirm-email?userId=&token=
  confirmEmail(userId: string, token: string): Observable<ApiResponseDTO> {
    return this.http.get<ApiResponseDTO>(`${this.baseUrl}/confirm-email`, {
      params: { userId, token },
    });
  }

  //POST /api/auth/login
  login(dto: AuthDTO.LoginDTO): Observable<AuthDTO.AuthResponseDTO> {
    return this.http
      .post<AuthDTO.AuthResponseDTO>(`${this.baseUrl}/login`, dto)
      .pipe(tap((res) => { this.saveTokens(res.token, res.refreshToken); this._scheduleWarning?.(); }));
  }

  //POST /api/auth/refresh
  refresh(dto: AuthDTO.RefreshTokenDTO): Observable<AuthDTO.AuthResponseDTO> {
    return this.http
      .post<AuthDTO.AuthResponseDTO>(`${this.baseUrl}/refresh`, dto)
      .pipe(tap((res) => { this.saveTokens(res.token, res.refreshToken); this._scheduleWarning?.(); }));
  }

  //POST /api/auth/logout  — requires Bearer token (attach via interceptor)
  logout(): Observable<ApiResponseDTO> {
    return this.http
      .post<ApiResponseDTO>(`${this.baseUrl}/logout`, {})
      .pipe(tap(() => this.clearTokens()));
  }

  // POST /api/auth/change-password
  changePassword(currentPassword: string, newPassword: string): Observable<ApiResponseDTO> {
    return this.http.post<ApiResponseDTO>(`${this.baseUrl}/change-password`, {
      currentPassword,
      newPassword,
    });
  }

  //POST /api/auth/forgot-password
  forgotPassword(email: string): Observable<ApiResponseDTO> {
    const dto: AuthDTO.ForgotPasswordDTO = { email };
    return this.http.post<ApiResponseDTO>(`${this.baseUrl}/forgot-password`, dto);
  }

  //POST /api/auth/reset-password
  resetPassword(dto: AuthDTO.ResetPasswordDTO): Observable<ApiResponseDTO> {
    return this.http.post<ApiResponseDTO>(`${this.baseUrl}/reset-password`, dto);
  }

  //POST /api/auth/resend-confirmation
  resendConfirmation(email: string): Observable<ApiResponseDTO> {
    const dto: AuthDTO.ForgotPasswordDTO = { email };
    return this.http.post<ApiResponseDTO>(`${this.baseUrl}/resend-confirmation`, dto);
  }

  isAdmin(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      return Array.isArray(roles) ? roles.includes('Admin') : roles === 'Admin';
    } catch {
      return false;
    }
  }

  //Token helpers

  saveTokens(token: string, refreshToken: string): void {
    localStorage.setItem('token', token);
    localStorage.setItem('refreshToken', refreshToken);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  clearTokens(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }
}