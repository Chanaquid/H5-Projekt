import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotificationDTO } from '../dtos/notificationDTO';


@Injectable({
  providedIn: 'root'
})
export class NotificationService {

  private baseUrl = 'https://localhost:7183/api/notifications';

  constructor(private http: HttpClient) {}

  //GET summary (unread count + 10 recent)
  getSummary(): Observable<NotificationDTO.NotificationSummaryDTO> {
    return this.http.get<NotificationDTO.NotificationSummaryDTO>(`${this.baseUrl}/summary`);
  }

  //GET all notifications
  getAll(): Observable<NotificationDTO.NotificationResponseDTO[]> {
    return this.http.get<NotificationDTO.NotificationResponseDTO[]>(this.baseUrl);
  }

  //PATCH mark one as read
  markAsRead(id: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/read`, {});
  }

  //PATCH mark all as read
  markAllAsRead(): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/read-all`, {});
  }
}