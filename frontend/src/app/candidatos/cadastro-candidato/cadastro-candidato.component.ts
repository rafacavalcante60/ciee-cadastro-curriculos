import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { CandidatoService } from '../candidato.service';

/**
 * Formulário de cadastro de candidato. É o único formulário da aplicação: o
 * cadastro manual e o cadastro com PDF usam este mesmo componente.
 */
@Component({
  selector: 'app-cadastro-candidato',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './cadastro-candidato.component.html',
  styleUrl: './cadastro-candidato.component.scss'
})
export class CadastroCandidatoComponent {
  private readonly servico = inject(CandidatoService);
  private readonly router = inject(Router);

  readonly formulario = inject(FormBuilder).nonNullable.group({
    nomeCompleto: ['', Validators.required],
    email: ['', Validators.required],
    telefone: [''],
    areaOuCargoDeInteresse: [''],
    resumoProfissional: ['']
  });

  readonly salvando = signal(false);
  readonly falhaAoSalvar = signal(false);

  salvar(): void {
    // Segundo clique ou Enter durante o salvamento não gera um segundo cadastro.
    if (this.salvando()) {
      return;
    }

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const valor = this.formulario.getRawValue();
    this.salvando.set(true);
    this.falhaAoSalvar.set(false);

    this.servico.criar({
      nomeCompleto: valor.nomeCompleto,
      email: valor.email,
      telefone: opcional(valor.telefone),
      areaOuCargoDeInteresse: opcional(valor.areaOuCargoDeInteresse),
      resumoProfissional: opcional(valor.resumoProfissional)
    }).subscribe({
      next: () => this.router.navigateByUrl('/candidatos'),
      error: () => {
        this.salvando.set(false);
        this.falhaAoSalvar.set(true);
      }
    });
  }
}

/** Campo opcional deixado em branco é gravado como ausente, não como texto vazio. */
function opcional(valor: string): string | null {
  return valor.trim() === '' ? null : valor;
}
