import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { Candidato } from '../candidato.model';
import { CandidatoService } from '../candidato.service';
import { TelefonePipe } from '../telefone.pipe';

type Situacao = 'carregando' | 'carregado' | 'naoEncontrado' | 'falha';

@Component({
  selector: 'app-detalhes-candidato',
  imports: [DatePipe, RouterLink, TelefonePipe, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './detalhes-candidato.component.html',
  styleUrl: './detalhes-candidato.component.scss'
})
export class DetalhesCandidatoComponent implements OnInit {
  private readonly servico = inject(CandidatoService);
  private readonly rota = inject(ActivatedRoute);

  readonly situacao = signal<Situacao>('carregando');
  readonly candidato = signal<Candidato | null>(null);

  ngOnInit(): void {
    const id = Number(this.rota.snapshot.paramMap.get('id'));

    // A API só aceita inteiro na rota; outro valor nem chegaria a um ProblemDetails.
    if (!Number.isInteger(id) || id <= 0) {
      this.situacao.set('naoEncontrado');
      return;
    }

    this.servico.detalhar(id).subscribe({
      next: candidato => {
        this.candidato.set(candidato);
        this.situacao.set('carregado');
      },
      error: (erro: HttpErrorResponse) => {
        this.situacao.set(erro.status === 404 ? 'naoEncontrado' : 'falha');
      }
    });
  }
}
