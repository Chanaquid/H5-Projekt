import { Injectable } from '@angular/core';
import { ItemDTO } from '../dtos/itemDTO';
import { Observable } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})

export class ItemService {
  private readonly baseUrl = 'https://localhost:7183/api/items';

  constructor(private http: HttpClient) {}

  // GET: api/items
  getAll(): Observable<ItemDTO.ItemSummaryDTO[]> {
    return this.http.get<ItemDTO.ItemSummaryDTO[]>(this.baseUrl);
  }

  // GET: api/items/admin/all
  getAllAdmin(includeInactive: boolean = false): Observable<ItemDTO.ItemSummaryDTO[]> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ItemDTO.ItemSummaryDTO[]>(`${this.baseUrl}/admin/all`, { params });
  }

  // GET: api/items/{id}
  getById(id: number): Observable<ItemDTO.ItemDetailDTO> {
    return this.http.get<ItemDTO.ItemDetailDTO>(`${this.baseUrl}/${id}`);
  }

  // GET: api/items/my
  getMyItems(): Observable<ItemDTO.ItemSummaryDTO[]> {
    return this.http.get<ItemDTO.ItemSummaryDTO[]>(`${this.baseUrl}/my`);
  }

  // GET: api/items/user/{userId}
  getByUser(userId: string): Observable<ItemDTO.ItemSummaryDTO[]> {
    return this.http.get<ItemDTO.ItemSummaryDTO[]>(`${this.baseUrl}/user/${userId}`);
  }

  // GET: api/items/user/{userId}/public
  getByUserPublic(userId: string): Observable<ItemDTO.ItemSummaryDTO[]> {
    return this.http.get<ItemDTO.ItemSummaryDTO[]>(`${this.baseUrl}/user/${userId}/public`);
  }

  // GET: api/items/nearby
  getNearby(lat: number, lng: number, radiusKm: number = 10): Observable<ItemDTO.ItemSummaryDTO[]> {
    const params = new HttpParams()
      .set('lat', lat.toString())
      .set('lng', lng.toString())
      .set('radiusKm', radiusKm.toString());
    return this.http.get<ItemDTO.ItemSummaryDTO[]>(`${this.baseUrl}/nearby`, { params });
  }

  // POST: api/items
  create(dto: ItemDTO.CreateItemDTO): Observable<ItemDTO.ItemDetailDTO> {
    return this.http.post<ItemDTO.ItemDetailDTO>(this.baseUrl, dto);
  }

  // PUT: api/items/{id}
  update(id: number, dto: ItemDTO.UpdateItemDTO): Observable<ItemDTO.ItemDetailDTO> {
    return this.http.put<ItemDTO.ItemDetailDTO>(`${this.baseUrl}/${id}`, dto);
  }

  // PATCH: api/items/admin/{id}/status
  updateStatus(id: number, dto: ItemDTO.AdminItemStatusDTO): Observable<ItemDTO.ItemDetailDTO> {
    return this.http.patch<ItemDTO.ItemDetailDTO>(`${this.baseUrl}/admin/${id}/status`, dto);
  }

  // DELETE: api/items/{id}
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // GET: api/items/{id}/qrcode
  getQrCode(id: number): Observable<ItemDTO.ItemQrCodeDTO> {
    return this.http.get<ItemDTO.ItemQrCodeDTO>(`${this.baseUrl}/${id}/qrcode`);
  }

  // POST: api/items/scan?qrCode=...
  scan(qrCode: string): Observable<ItemDTO.ItemDetailDTO> {
    const params = new HttpParams().set('qrCode', qrCode);
    return this.http.post<ItemDTO.ItemDetailDTO>(`${this.baseUrl}/scan`, {}, { params });
  }

  // GET: api/items/admin/pending
  getPendingApprovals(): Observable<ItemDTO.AdminPendingItemDTO[]> {
    return this.http.get<ItemDTO.AdminPendingItemDTO[]>(`${this.baseUrl}/admin/pending`);
  }

  // POST: api/items/admin/{id}/decide
  adminDecide(id: number, dto: ItemDTO.AdminItemDecisionDTO): Observable<ItemDTO.ItemDetailDTO> {
    return this.http.post<ItemDTO.ItemDetailDTO>(`${this.baseUrl}/admin/${id}/decide`, dto);
  }
}