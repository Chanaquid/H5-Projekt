import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminDTO } from '../dtos/adminDTO';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly baseUrl = 'https://localhost:7183/api/admin';
  constructor(private http: HttpClient) {}

  getDashboard(): Observable<AdminDTO.AdminDashboardDTO> {
    return this.http.get<AdminDTO.AdminDashboardDTO>(`${this.baseUrl}/dashboard`);
  }

  getItemHistory(itemId: number): Observable<AdminDTO.ItemHistoryDTO> {
    return this.http.get<AdminDTO.ItemHistoryDTO>(`${this.baseUrl}/items/${itemId}/history`);
  }
}