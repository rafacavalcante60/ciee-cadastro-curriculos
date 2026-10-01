import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';

import { CadastroCandidatoComponent } from './cadastro-candidato.component';
import { CandidatoService } from '../candidato.service';
import { Candidato, NovoCandidato } from '../candidato.model';

@Component({ template: '' })
class ListagemFalsaComponent {}

describe('CadastroCandidatoComponent', () => {
  let fixture: ComponentFixture<CadastroCandidatoComponent>;
  let tela: HTMLElement;
  let respostaDoServidor: Subject<Candidato>;
  let enviados: NovoCandidato[];

  beforeEach(() => {
    respostaDoServidor = new Subject<Candidato>();
    enviados = [];
    const servicoFalso: Partial<CandidatoService> = {
      criar: (novo: NovoCandidato) => {
        enviados.push(novo);
        return respostaDoServidor.asObservable();
      }
    };

    TestBed.configureTestingModule({
      imports: [CadastroCandidatoComponent],
      providers: [
        provideRouter([{ path: 'candidatos', component: ListagemFalsaComponent }]),
        provideNoopAnimations(),
        { provide: CandidatoService, useValue: servicoFalso }
      ]
    });

    fixture = TestBed.createComponent(CadastroCandidatoComponent);
    tela = fixture.nativeElement;
    fixture.detectChanges();
  });

  function preencher(campo: string, valor: string): void {
    const elemento = tela.querySelector<HTMLInputElement | HTMLTextAreaElement>(`[formControlName="${campo}"]`)!;
    elemento.value = valor;
    elemento.dispatchEvent(new Event('input'));
  }

  function botaoSalvar(): HTMLButtonElement {
    return tela.querySelector<HTMLButtonElement>('button[type="submit"]')!;
  }

  function enviar(): void {
    tela.querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  it('envia os cinco campos, com os opcionais em branco como null', () => {
    preencher('nomeCompleto', 'Maria da Silva');
    preencher('email', 'maria.silva@exemplo.com');
    preencher('telefone', '(11) 98765-4321');
    preencher('areaOuCargoDeInteresse', '   ');

    enviar();

    expect(enviados).toEqual([{
      nomeCompleto: 'Maria da Silva',
      email: 'maria.silva@exemplo.com',
      telefone: '(11) 98765-4321',
      areaOuCargoDeInteresse: null,
      resumoProfissional: null
    }]);
  });

  it('enquanto salva, desabilita o botão e ignora um segundo envio', () => {
    preencher('nomeCompleto', 'Maria da Silva');
    preencher('email', 'maria.silva@exemplo.com');

    enviar();
    enviar();

    expect(enviados.length).toBe(1);
    expect(botaoSalvar().disabled).toBeTrue();
    expect(botaoSalvar().textContent).toContain('Salvando');
  });

  it('depois de salvar, volta para a listagem', async () => {
    preencher('nomeCompleto', 'Maria da Silva');
    preencher('email', 'maria.silva@exemplo.com');

    enviar();
    respostaDoServidor.next({ id: 1 } as Candidato);
    await fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/candidatos');
  });

  it('se o salvamento falhar, libera o botão para nova tentativa e avisa', () => {
    preencher('nomeCompleto', 'Maria da Silva');
    preencher('email', 'maria.silva@exemplo.com');

    enviar();
    respostaDoServidor.error(new Error('falha de rede'));
    fixture.detectChanges();

    expect(botaoSalvar().disabled).toBeFalse();
    expect(tela.textContent).toContain('Não foi possível salvar');
  });
});
