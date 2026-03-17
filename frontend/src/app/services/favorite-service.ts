import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ItemDTO } from '../dtos/itemDTO';
import { FavoriteDTO } from '../dtos/favoriteDTO';

@Injectable({
  providedIn: 'root',
})
export class FavoriteService {

  private readonly baseUrl = 'https://localhost:7183/api/favorites';

  constructor(private http: HttpClient) {}

  getMyFavorites(): Observable<FavoriteDTO.FavoriteResponseDTO[]> {
    return this.http.get<FavoriteDTO.FavoriteResponseDTO[]>(this.baseUrl);
  }

  addFavorite(itemId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${itemId}`, {});
  }

  removeFavorite(itemId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${itemId}`);
  }


}
