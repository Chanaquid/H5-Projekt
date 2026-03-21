import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DisputeDTO } from '../dtos/disputeDTO';

@Injectable({
  providedIn: 'root',
})
export class DisputeService {

  private readonly baseUrl = 'https://localhost:7183/api/disputes';


  constructor(private http: HttpClient) {}

  // GET my disputes
  getMyDisputes(): Observable<DisputeDTO.DisputeSummaryDTO[]> {
    return this.http.get<DisputeDTO.DisputeSummaryDTO[]>(`${this.baseUrl}/my`);
  }

  // GET dispute by id
  getById(id: number): Observable<DisputeDTO.DisputeDetailDTO> {
    return this.http.get<DisputeDTO.DisputeDetailDTO>(`${this.baseUrl}/${id}`);
  }

  getDisputeHistoryByItemId(itemId: number): Observable<DisputeDTO.DisputeSummaryDTO[]> {
  return this.http.get<any[]>(`${this.baseUrl}/item/${itemId}`);
}

  // POST create dispute
  create(dto: DisputeDTO.CreateDisputeDTO): Observable<DisputeDTO.DisputeDetailDTO> {
    return this.http.post<DisputeDTO.DisputeDetailDTO>(this.baseUrl, dto);
  }

  // POST respond to dispute
  respond(id: number, dto: DisputeDTO.DisputeResponseDTO): Observable<DisputeDTO.DisputeDetailDTO> {
    return this.http.post<DisputeDTO.DisputeDetailDTO>(`${this.baseUrl}/${id}/respond`, dto);
  }

  // POST add photo
  addPhoto(id: number, dto: DisputeDTO.AddDisputePhotoDTO): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/photos`, dto);
  }

  // GET all open disputes — admin
  getAllOpen(): Observable<DisputeDTO.DisputeSummaryDTO[]> {
    return this.http.get<DisputeDTO.DisputeSummaryDTO[]>(`${this.baseUrl}/admin/open`);
  }

  // POST issue verdict — admin
  issueVerdict(id: number, dto: DisputeDTO.AdminVerdictDTO): Observable<DisputeDTO.DisputeDetailDTO> {
    return this.http.post<DisputeDTO.DisputeDetailDTO>(`${this.baseUrl}/admin/${id}/verdict`, dto);
  }




}
