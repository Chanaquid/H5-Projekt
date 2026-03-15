import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LoanDTO } from '../dtos/loanDTO';

@Injectable({
  providedIn: 'root'
})
export class LoanService {
  private readonly baseUrl = 'https://localhost:7183/api/loans';

  constructor(private http: HttpClient) {}

  // GET — loans where I am the borrower
  getBorrowedLoans(): Observable<LoanDTO.LoanSummaryDTO[]> {
    return this.http.get<LoanDTO.LoanSummaryDTO[]>(`${this.baseUrl}/borrowed`);
  }

  // GET — loan requests on my items
  getOwnedLoans(): Observable<LoanDTO.LoanSummaryDTO[]> {
    return this.http.get<LoanDTO.LoanSummaryDTO[]>(`${this.baseUrl}/owned`);
  }

  // GET - get loan by id
  getById(id: number): Observable<LoanDTO.LoanDetailDTO> {
    return this.http.get<LoanDTO.LoanDetailDTO>(`${this.baseUrl}/${id}`);
  }

  // POST — borrower requests a loan
  createLoan(dto: LoanDTO.CreateLoanDTO): Observable<LoanDTO.LoanDetailDTO> {
    return this.http.post<LoanDTO.LoanDetailDTO>(this.baseUrl, dto);
  }

  // POST — borrower cancels their own pending/approved loan
  cancelLoan(id: number, dto: LoanDTO.CancelLoanDTO): Observable<LoanDTO.LoanDetailDTO> {
    return this.http.post<LoanDTO.LoanDetailDTO>(`${this.baseUrl}/${id}/cancel`, dto);
  }

  // POST — owner approves or rejects a loan request
  decideLoan(id: number, dto: LoanDTO.LoanDecisionDTO): Observable<LoanDTO.LoanDetailDTO> {
    return this.http.post<LoanDTO.LoanDetailDTO>(`${this.baseUrl}/${id}/decide`, dto);
  }

  // POST — borrower requests extended end date
  requestExtension(id: number, dto: LoanDTO.RequestExtensionDTO): Observable<LoanDTO.LoanDetailDTO> {
    return this.http.post<LoanDTO.LoanDetailDTO>(`${this.baseUrl}/${id}/request-extension`, dto);
  }

  // POST — owner approves or rejects extension
  decideExtension(id: number, dto: LoanDTO.ExtensionDecisionDTO): Observable<LoanDTO.LoanDetailDTO> {
    return this.http.post<LoanDTO.LoanDetailDTO>(`${this.baseUrl}/${id}/decide-extension`, dto);
  }

  // --- ADMIN ENDPOINTS ---

  getAllLoans(): Observable<LoanDTO.LoanSummaryDTO[]> {
    return this.http.get<LoanDTO.LoanSummaryDTO[]>(`${this.baseUrl}/admin/all`);
  }

  getPendingAdminApprovals(): Observable<LoanDTO.AdminPendingLoanDTO[]> {
    return this.http.get<LoanDTO.AdminPendingLoanDTO[]>(`${this.baseUrl}/admin/pending`);
  }

  adminDecide(id: number, dto: LoanDTO.LoanDecisionDTO): Observable<LoanDTO.LoanDetailDTO> {
    return this.http.post<LoanDTO.LoanDetailDTO>(`${this.baseUrl}/admin/${id}/decide`, dto);
  }
}