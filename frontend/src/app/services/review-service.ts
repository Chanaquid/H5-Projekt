import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, Observable, of } from 'rxjs';
import { ReviewDTO } from '../dtos/reviewDTO';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly baseUrl = 'https://localhost:7183/api/reviews';
  constructor(private http: HttpClient) {}

  getItemReviews(itemId: number): Observable<ReviewDTO.ItemReviewResponseDTO[]> {
    return this.http.get<ReviewDTO.ItemReviewResponseDTO[]>(`${this.baseUrl}/items/${itemId}`);
  }

  getUserReviews(userId: string): Observable<ReviewDTO.UserReviewResponseDTO[]> {
    return this.http.get<ReviewDTO.UserReviewResponseDTO[]>(`${this.baseUrl}/users/${userId}`);
  }
  
  getItemReviewsByLoan(loanId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/items/loan/${loanId}`).pipe(
      catchError(() => of([]))
    );
  }

  createItemReview(dto: { loanId: number; itemId: number; rating: number; comment?: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/items`, dto);
  }

  createUserReview(dto: { loanId: number; reviewedUserId: string; rating: number; comment?: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/users`, dto);
  }


}