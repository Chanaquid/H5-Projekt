import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserDTO } from '../dtos/userDTO';


@Injectable({
  providedIn: 'root',
})

export class UserService {
  private readonly baseUrl = 'https://localhost:7183/api/users';

  constructor(private http: HttpClient) {}

  // GET: Get your own profile
  getMe(): Observable<UserDTO.UserProfileDTO> {
    return this.http.get<UserDTO.UserProfileDTO>(`${this.baseUrl}/me`);
  }


  // GET: Get a public profile (accessible by any auth user)
  getPublicProfile(id: string): Observable<UserDTO.UserSummaryDTO> {
    return this.http.get<UserDTO.UserSummaryDTO>(`${this.baseUrl}/${id}/profile`);
  }

  getScoreHistoryByLoanId(loanId: number): Observable<UserDTO.ScoreHistoryDTO[]> {
    return this.http.get<UserDTO.ScoreHistoryDTO[]>(`${this.baseUrl}/score-history/loan/${loanId}`);
  }

  // PUT: Update your own profile
  updateProfile(dto: UserDTO.UpdateProfileDTO): Observable<UserDTO.UserProfileDTO> {
    return this.http.put<UserDTO.UserProfileDTO>(`${this.baseUrl}/me`, dto);
  }

  // DELETE: Delete your own account
  deleteAccount(dto: UserDTO.DeleteAccountDTO): Observable<{ message: string }> {
    return this.http.request<{ message: string }>('delete', `${this.baseUrl}/me`, { body: dto });
  }

  // GET: Get your own score history
  getScoreHistory(): Observable<UserDTO.ScoreHistoryDTO[]> {
    return this.http.get<UserDTO.ScoreHistoryDTO[]>(`${this.baseUrl}/me/score-history`);
  }

  // ================= ADMIN ENDPOINTS =================

  // GET: Get all users (Admin only)
  getAllUsers(): Observable<UserDTO.AdminUserDTO[]> {
    return this.http.get<UserDTO.AdminUserDTO[]>(this.baseUrl);
  }

  // GET: Get full user details by ID (Admin only)
  getUserById(id: string): Observable<any> { // Use AdminUserDetailDTO if fully defined
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }

  // POST: Manually adjust a user's score (Admin only)
  adjustScore(id: string, dto: UserDTO.AdminScoreAdjustDTO): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/adjust-score`, dto);
  }

  // PUT: Edit any user's profile/account (Admin only)
  adminEditUser(id: string, dto: UserDTO.AdminEditUserDTO): Observable<UserDTO.AdminUserDTO> {
    return this.http.put<UserDTO.AdminUserDTO>(`${this.baseUrl}/${id}`, dto);
  }

  // DELETE: Delete any user (Admin only)
  adminDeleteUser(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${id}`);
  }
}