import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ChatDTO } from '../dtos/chatDTO';

@Injectable({ providedIn: 'root' })
export class LoanMessageService {
  private readonly baseUrl = 'https://localhost:7183/api/messages/loan';

  constructor(private http: HttpClient) {}

  // GET /api/messages/loan/{loanId}
  getThread(loanId: number): Observable<ChatDTO.LoanMessageDTO.LoanMessageThreadDTO> {
    return this.http.get<ChatDTO.LoanMessageDTO.LoanMessageThreadDTO>(`${this.baseUrl}/${loanId}/thread`);
  }

  // POST /api/messages/loan
  send(dto: ChatDTO.LoanMessageDTO.SendLoanMessageDTO): Observable<ChatDTO.LoanMessageDTO.LoanMessageResponseDTO> {
    return this.http.post<ChatDTO.LoanMessageDTO.LoanMessageResponseDTO>(this.baseUrl, dto);
  }

  // PATCH /api/messages/loan/{loanId}/read
  markThreadAsRead(loanId: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${loanId}/read`, {});
  }
}