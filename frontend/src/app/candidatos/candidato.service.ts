import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Candidato, NovoCandidato } from './candidato.model';

/**
 * Acesso HTTP aos candidatos. Usa caminho relativo: em desenvolvimento o proxy
 * do servidor do Angular repassa /api para a API, e por isso não há CORS.
 */
@Injectable({ providedIn: 'root' })
export class CandidatoService {
  private readonly http = inject(HttpClient);
  private readonly endereco = '/api/candidatos';

  criar(novo: NovoCandidato): Observable<Candidato> {
    return this.http.post<Candidato>(this.endereco, novo);
  }

  /** Candidatos dos mais recentes para os mais antigos, na ordem da API. */
  listar(): Observable<Candidato[]> {
    return this.http.get<Candidato[]>(this.endereco);
  }
}
