import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { FineDTO } from '../dtos/fineDTO';

@Injectable({
  providedIn: 'root',
})
export class FineService {
  private readonly baseUrl = 'https://localhost:7183/api/fines';

  constructor(private http: HttpClient) {}

  //GET /api/fines/my — logged-in user's own fines
  getMyFines(): Observable<FineDTO.FineResponseDTO[]> {
    return this.http.get<FineDTO.FineResponseDTO[]>(`${this.baseUrl}/my`);
  }

  // POST /api/fines/pay — user submits payment proof
  markAsPaid(dto: FineDTO.PayFineDTO): Observable<FineDTO.FineResponseDTO> {
    return this.http.post<FineDTO.FineResponseDTO>(`${this.baseUrl}/pay`, dto);
  }

  //Admin endpoints

  // GET /api/fines/admin/unpaid
  getAdminUnpaid(): Observable<FineDTO.FineResponseDTO[]> {
    return this.http.get<FineDTO.FineResponseDTO[]>(`${this.baseUrl}/admin/unpaid`);
  }

  // GET /api/fines/admin/pending-verification
  getAdminPendingVerification(): Observable<FineDTO.FineResponseDTO[]> {
    return this.http.get<FineDTO.FineResponseDTO[]>(`${this.baseUrl}/admin/pending-verification`);
  }

  // GET /api/fines/admin/user/:userId
  getAdminFinesByUser(userId: string): Observable<FineDTO.FineResponseDTO[]> {
    return this.http.get<FineDTO.FineResponseDTO[]>(`${this.baseUrl}/admin/user/${userId}`);
  }

  // GET /api/fines/dispute/:disputeId
  getFinesByDisputeId(disputeId: number): Observable<FineDTO.FineResponseDTO[]> {
    return this.http.get<FineDTO.FineResponseDTO[]>(`${this.baseUrl}/dispute/${disputeId}`);
  }

  // POST /api/fines/admin/issue
  adminIssueFine(dto: FineDTO.AdminIssueFineDTO): Observable<FineDTO.FineResponseDTO> {
    return this.http.post<FineDTO.FineResponseDTO>(`${this.baseUrl}/admin/issue`, dto);
  }

  // POST /api/fines/admin/confirm-payment
  adminConfirmPayment(dto: FineDTO.AdminFineVerificationDTO): Observable<FineDTO.FineResponseDTO> {
    return this.http.post<FineDTO.FineResponseDTO>(`${this.baseUrl}/admin/confirm-payment`, dto);
  }

  // PUT /api/fines/admin/:fineId
  adminUpdateFine(fineId: number, dto: FineDTO.AdminUpdateFineDTO): Observable<FineDTO.FineResponseDTO> {
    return this.http.put<FineDTO.FineResponseDTO>(`${this.baseUrl}/admin/${fineId}`, dto);
  }
}