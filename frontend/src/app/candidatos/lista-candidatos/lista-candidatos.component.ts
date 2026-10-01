import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';

import { Candidato } from '../candidato.model';
import { CandidatoService } from '../candidato.service';

/** Listagem dos candidatos, dos cadastrados mais recentemente para os mais antigos. */
@Component({
  selector: 'app-lista-candidatos',
  imports: [DatePipe, RouterLink, MatButtonModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './lista-candidatos.component.html',
  styleUrl: './lista-candidatos.component.scss'
})
export class ListaCandidatosComponent implements OnInit {
  private readonly servico = inject(CandidatoService);

  readonly colunas = ['nomeCompleto', 'email', 'dataCadastro'];
  readonly candidatos = signal<Candidato[]>([]);
  readonly carregando = signal(true);
  readonly falhaAoCarregar = signal(false);

  ngOnInit(): void {
    this.servico.listar().subscribe({
      next: candidatos => {
        this.candidatos.set(candidatos);
        this.carregando.set(false);
      },
      error: () => {
        this.falhaAoCarregar.set(true);
        this.carregando.set(false);
      }
    });
  }
}
