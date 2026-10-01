import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Candidato, NovoCandidato } from './candidato.model';

// Caminho relativo: em desenvolvimento o proxy do Angular repassa /api, sem CORS.
@Injectable({ providedIn: 'root' })
export class CandidatoService {
  private readonly http = inject(HttpClient);
  private readonly endereco = '/api/candidatos';

  criar(novo: NovoCandidato): Observable<Candidato> {
    return this.http.post<Candidato>(this.endereco, novo);
  }

  listar(): Observable<Candidato[]> {
    return this.http.get<Candidato[]>(this.endereco);
  }
}
