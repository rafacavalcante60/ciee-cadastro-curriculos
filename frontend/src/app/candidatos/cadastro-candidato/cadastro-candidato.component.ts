import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { CandidatoService } from '../candidato.service';
import { FalhaAoCadastrar } from '../candidato.model';

// Único formulário da aplicação: o cadastro manual e o com PDF usam este componente.
// As regras e as mensagens repetem as da API, que é a autoridade final.
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
  private readonly avisos = inject(MatSnackBar);

  readonly formulario = inject(FormBuilder).nonNullable.group({
    nomeCompleto: ['', [obrigatorio, minimoDeCaracteres(2), Validators.maxLength(200)]],
    email: ['', [obrigatorio, Validators.pattern(/^\s*[^\s@]+@[^\s@]+\.[^\s@]+\s*$/), Validators.maxLength(256)]],
    telefone: ['', telefone],
    areaOuCargoDeInteresse: ['', Validators.maxLength(120)],
    resumoProfissional: ['', Validators.maxLength(2000)]
  });

  readonly salvando = signal(false);
  readonly falha = signal<Exclude<FalhaAoCadastrar['tipo'], 'campos'> | null>(null);

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
    this.falha.set(null);

    this.servico.criar({
      nomeCompleto: valor.nomeCompleto,
      email: valor.email,
      telefone: nuloSeEmBranco(valor.telefone),
      areaOuCargoDeInteresse: nuloSeEmBranco(valor.areaOuCargoDeInteresse),
      resumoProfissional: nuloSeEmBranco(valor.resumoProfissional)
    }).subscribe({
      next: () => {
        this.avisos.open('Candidato cadastrado com sucesso.', 'Fechar', { duration: 5000 });
        this.router.navigateByUrl('/candidatos');
      },
      error: (falha: FalhaAoCadastrar) => {
        this.salvando.set(false);
        this.mostrarFalha(falha);
      }
    });
  }

  mensagemDeErro(campo: keyof typeof this.formulario.controls): string {
    const erros = this.formulario.controls[campo].errors;
    if (!erros) {
      return '';
    }
    if (erros['api']) {
      return erros['api'];
    }
    if (erros['obrigatorio']) {
      return campo === 'email' ? 'Informe o e-mail.' : 'Informe o nome completo.';
    }
    if (erros['minimo']) {
      return 'O nome completo deve ter ao menos 2 caracteres.';
    }
    if (erros['maxlength']) {
      return `Use no máximo ${erros['maxlength'].requiredLength} caracteres.`;
    }
    if (erros['pattern']) {
      return 'Informe um e-mail válido.';
    }
    if (erros['telefone']) {
      return 'Informe o telefone com DDD, com 10 ou 11 dígitos.';
    }
    return '';
  }

  // O erro vindo da API fica no controle até o valor mudar: aí o Angular roda os
  // validadores de novo e o substitui.
  private mostrarFalha(falha: FalhaAoCadastrar): void {
    if (falha.tipo !== 'campos') {
      this.falha.set(falha.tipo);
      return;
    }

    let algumNoFormulario = false;
    for (const [campo, mensagem] of Object.entries(falha.erros)) {
      const controle = this.formulario.get(campo);
      if (controle) {
        controle.setErrors({ api: mensagem });
        controle.markAsTouched();
        algumNoFormulario = true;
      }
    }
    if (!algumNoFormulario) {
      this.falha.set('inesperada');
    }
  }
}

// Validators.required aceita só espaços; a API não.
function obrigatorio(controle: AbstractControl<string>): ValidationErrors | null {
  return controle.value.trim() === '' ? { obrigatorio: true } : null;
}

// Validators.minLength conta os espaços das pontas; a API grava o nome aparado.
function minimoDeCaracteres(minimo: number): ValidatorFn {
  return controle => {
    const texto = controle.value.trim();
    return texto !== '' && texto.length < minimo ? { minimo: true } : null;
  };
}

// Mesma normalização da API: descarta máscara e separadores, o resto tem de ser dígito.
function telefone(controle: AbstractControl<string>): ValidationErrors | null {
  if (controle.value.trim() === '') {
    return null;
  }
  const normalizado = controle.value.replace(/[\s().-]/g, '');
  return /^[0-9]{10,11}$/.test(normalizado) ? null : { telefone: true };
}

/** Campo opcional deixado em branco é gravado como ausente, não como texto vazio. */
function nuloSeEmBranco(valor: string): string | null {
  return valor.trim() === '' ? null : valor;
}
