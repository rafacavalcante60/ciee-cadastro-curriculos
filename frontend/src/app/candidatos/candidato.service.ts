import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';

import { CamposExtraidos, Candidato, FalhaAoCadastrar, FalhaNaExtracao, NovoCandidato } from './candidato.model';

// Caminho relativo: em desenvolvimento o proxy do Angular repassa /api, sem CORS.
@Injectable({ providedIn: 'root' })
export class CandidatoService {
  private readonly http = inject(HttpClient);
  private readonly endereco = '/api/candidatos';

  // Em caso de erro, o Observable falha com um FalhaAoCadastrar.
  criar(novo: NovoCandidato): Observable<Candidato> {
    return this.http.post<Candidato>(this.endereco, novo).pipe(
      catchError((erro: HttpErrorResponse) => throwError(() => traduzirFalha(erro)))
    );
  }

  listar(): Observable<Candidato[]> {
    return this.http.get<Candidato[]>(this.endereco);
  }

  detalhar(id: number): Observable<Candidato> {
    return this.http.get<Candidato>(`${this.endereco}/${id}`);
  }

  // Em caso de erro, o Observable falha com um FalhaNaExtracao.
  extrairCurriculo(arquivo: File): Observable<CamposExtraidos> {
    const formulario = new FormData();
    formulario.append('arquivo', arquivo);
    return this.http.post<CamposExtraidos>('/api/curriculos/extracao', formulario).pipe(
      catchError((erro: HttpErrorResponse) => throwError(() => traduzirFalhaNaExtracao(erro)))
    );
  }
}

function traduzirFalha(erro: HttpErrorResponse): FalhaAoCadastrar {
  if (erro.status === 400 && erro.error?.errors) {
    const erros: Record<string, string> = {};
    for (const [campo, mensagens] of Object.entries<string[]>(erro.error.errors)) {
      erros[campo] = mensagens[0];
    }
    return { tipo: 'campos', erros };
  }

  if (erro.status === 409) {
    return { tipo: 'campos', erros: { email: erro.error?.detail ?? 'Já existe um candidato cadastrado com este e-mail.' } };
  }

  // Com a API fora do ar, o proxy de desenvolvimento responde 500 e o Nginx 502:
  // pelo status não dá para separar API caída de erro interno.
  if (erro.status === 0 || erro.status >= 500) {
    return { tipo: 'servidorIndisponivel' };
  }

  return { tipo: 'inesperada' };
}

function traduzirFalhaNaExtracao(erro: HttpErrorResponse): FalhaNaExtracao {
  if (erro.status === 400) {
    return { tipo: 'arquivoInvalido', mensagem: erro.error?.detail ?? 'O arquivo enviado não pôde ser lido.' };
  }
  if (erro.status === 0 || erro.status >= 500) {
    return { tipo: 'servidorIndisponivel' };
  }
  return { tipo: 'inesperada' };
}
