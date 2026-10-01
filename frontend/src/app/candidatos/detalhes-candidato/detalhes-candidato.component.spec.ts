import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { DetalhesCandidatoComponent } from './detalhes-candidato.component';
import { CandidatoService } from '../candidato.service';
import { Candidato } from '../candidato.model';

describe('DetalhesCandidatoComponent', () => {
  const resumoLongo = 'Experiência com atendimento e rotinas administrativas. '.repeat(30).trim();

  const candidato: Candidato = {
    id: 7,
    nomeCompleto: 'Maria da Silva',
    email: 'maria.silva@exemplo.com',
    telefone: '11987654321',
    areaOuCargoDeInteresse: 'Desenvolvimento de software',
    resumoProfissional: resumoLongo,
    dataCadastro: '2026-03-10T14:30:00Z'
  };

  let idsPedidos: number[];

  function abrir(id: string, resposta: Observable<Candidato>): HTMLElement {
    idsPedidos = [];
    const servicoFalso: Partial<CandidatoService> = {
      detalhar: (pedido: number) => {
        idsPedidos.push(pedido);
        return resposta;
      }
    };

    TestBed.configureTestingModule({
      imports: [DetalhesCandidatoComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CandidatoService, useValue: servicoFalso },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id }) } } }
      ]
    });

    const fixture: ComponentFixture<DetalhesCandidatoComponent> = TestBed.createComponent(DetalhesCandidatoComponent);
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  it('busca o candidato da rota e exibe todos os campos, com o resumo por inteiro', () => {
    const tela = abrir('7', of(candidato));

    expect(idsPedidos).toEqual([7]);
    const texto = tela.textContent!;
    expect(texto).toContain('Maria da Silva');
    expect(texto).toContain('maria.silva@exemplo.com');
    expect(texto).toContain('11987654321');
    expect(texto).toContain('Desenvolvimento de software');
    expect(texto).toContain(resumoLongo);
  });

  it('campo opcional ausente aparece como não informado', () => {
    const tela = abrir('7', of({ ...candidato, telefone: null, resumoProfissional: null }));

    expect(tela.textContent).toContain('Não informado');
  });

  it('candidato inexistente mostra mensagem clara', () => {
    const tela = abrir('999', throwError(() => new HttpErrorResponse({ status: 404 })));

    expect(tela.querySelector('[role="alert"]')?.textContent).toContain('Candidato não encontrado');
  });

  it('identificador que não é número é tratado como inexistente, sem chamar a API', () => {
    const tela = abrir('abc', of(candidato));

    expect(idsPedidos).toEqual([]);
    expect(tela.querySelector('[role="alert"]')?.textContent).toContain('Candidato não encontrado');
  });

  it('outras falhas mostram mensagem de erro genérica', () => {
    const tela = abrir('7', throwError(() => new HttpErrorResponse({ status: 500 })));

    expect(tela.querySelector('[role="alert"]')?.textContent).toContain('Não foi possível carregar');
  });
});
