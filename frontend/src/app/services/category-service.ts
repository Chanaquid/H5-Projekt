import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CategoryDTO } from '../dtos/categoryDTO';

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private readonly baseUrl = 'https://localhost:7183/api/categories';

  constructor(private http: HttpClient) {}


  getAll(): Observable<CategoryDTO.CategoryResponseDTO[]> {
    return this.http.get<CategoryDTO.CategoryResponseDTO[]>(this.baseUrl);
  }


  getById(id: number): Observable<CategoryDTO.CategoryResponseDTO> {
    return this.http.get<CategoryDTO.CategoryResponseDTO>(`${this.baseUrl}/${id}`);
  }


  create(dto: CategoryDTO.CreateCategoryDTO): Observable<CategoryDTO.CategoryResponseDTO> {
    return this.http.post<CategoryDTO.CategoryResponseDTO>(this.baseUrl , dto);
  }

  
  update(id: number, dto: CategoryDTO.UpdateCategoryDTO): Observable<CategoryDTO.CategoryResponseDTO> {
    return this.http.put<CategoryDTO.CategoryResponseDTO>(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}