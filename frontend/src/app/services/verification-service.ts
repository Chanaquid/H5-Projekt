import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { VerificationDTO } from '../dtos/verificationDTO';

@Injectable({
  providedIn: 'root',
})
export class VerificationService {

  private readonly baseUrl = 'https://localhost:7183/api/verification';

  constructor(private http: HttpClient) { }

  getMyRequest(): Observable<VerificationDTO.VerificationRequestResponseDTO> {
    return this.http.get<VerificationDTO.VerificationRequestResponseDTO>(`${this.baseUrl}/my`);
  }

  submitRequest(dto: VerificationDTO.CreateVerificationRequestDTO): Observable<VerificationDTO.VerificationRequestResponseDTO> {
    return this.http.post<VerificationDTO.VerificationRequestResponseDTO>(this.baseUrl, dto);
  }


  getPendingRequests(): Observable<VerificationDTO.VerificationRequestResponseDTO[]> {
    return this.http.get<VerificationDTO.VerificationRequestResponseDTO[]>(`${this.baseUrl}/admin/pending`);
  }

  decideRequest(id: number, dto: VerificationDTO.AdminVerificationDecisionDTO): Observable<VerificationDTO.VerificationRequestResponseDTO> {
    return this.http.post<VerificationDTO.VerificationRequestResponseDTO>(`${this.baseUrl}/admin/${id}/decide`, dto);
  }

}
