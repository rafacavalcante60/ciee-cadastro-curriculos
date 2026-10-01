import { Component } from '@angular/core';
import { OverlayContainer } from '@angular/cdk/overlay';
import { ComponentFixture, TestBed, fakeAsync, flush } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';

import { CadastroCandidatoComponent } from './cadastro-candidato.component';
import { CandidatoService } from '../candidato.service';
import { CamposExtraidos, Candidato, FalhaAoCadastrar, NovoCandidato } from '../candidato.model';

@Component({ template: '' })
class ListagemFalsaComponent {}

describe('CadastroCandidatoComponent', () => {
  let fixture: ComponentFixture<CadastroCandidatoComponent>;
  let tela: HTMLElement;
  let respostaDoServidor: Subject<Candidato>;
  let enviados: NovoCandidato[];
  let respostaDaExtracao: Subject<CamposExtraidos>;
  let arquivosEnviados: File[];

  beforeEach(() => {
    respostaDoServidor = new Subject<Candidato>();
    enviados = [];
    respostaDaExtracao = new Subject<CamposExtraidos>();
    arquivosEnviados = [];
    const servicoFalso: Partial<CandidatoService> = {
      criar: (novo: NovoCandidato) => {
        enviados.push(novo);
        return respostaDoServidor.asObservable();
      },
      extrairCurriculo: (arquivo: File) => {
        arquivosEnviados.push(arquivo);
        return respostaDaExtracao.asObservable();
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

  function erroDoCampo(campo: string): string {
    const campoDoFormulario = tela.querySelector(`[formControlName="${campo}"]`)!.closest('mat-form-field')!;
    return campoDoFormulario.querySelector('mat-error')?.textContent?.trim() ?? '';
  }

  function falhar(falha: FalhaAoCadastrar): void {
    respostaDoServidor.error(falha);
    fixture.detectChanges();
  }

  function preencherValido(): void {
    preencher('nomeCompleto', 'Maria da Silva');
    preencher('email', 'maria.silva@exemplo.com');
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

  // fakeAsync em vez de whenStable: o aviso de sucesso fica aberto por um tempo,
  // e esse timer impediria a zona de estabilizar.
  it('depois de salvar, volta para a listagem', fakeAsync(() => {
    preencherValido();

    enviar();
    respostaDoServidor.next({ id: 1 } as Candidato);
    flush();

    expect(TestBed.inject(Router).url).toBe('/candidatos');
  }));

  it('se o salvamento falhar, libera o botão para nova tentativa e avisa', () => {
    preencherValido();

    enviar();
    falhar({ tipo: 'inesperada' });

    expect(botaoSalvar().disabled).toBeFalse();
    expect(tela.textContent).toContain('Não foi possível salvar');
  });

  it('confirma o cadastro com uma mensagem visível', fakeAsync(() => {
    preencherValido();

    enviar();
    respostaDoServidor.next({ id: 1 } as Candidato);
    fixture.detectChanges();

    const sobreposicao = TestBed.inject(OverlayContainer).getContainerElement();
    expect(sobreposicao.textContent).toContain('Candidato cadastrado com sucesso.');
    flush();
  }));

  describe('validação', () => {
    it('com nome e e-mail em branco, não envia e aponta os dois campos', () => {
      preencher('nomeCompleto', '   ');

      enviar();

      expect(enviados).toEqual([]);
      expect(erroDoCampo('nomeCompleto')).toBe('Informe o nome completo.');
      expect(erroDoCampo('email')).toBe('Informe o e-mail.');
    });

    it('exige ao menos 2 caracteres no nome, sem contar os espaços das pontas', () => {
      preencher('nomeCompleto', ' M ');
      preencher('email', 'maria@exemplo.com');

      enviar();

      expect(enviados).toEqual([]);
      expect(erroDoCampo('nomeCompleto')).toBe('O nome completo deve ter ao menos 2 caracteres.');
    });

    for (const email of ['maria', 'maria@', '@exemplo.com', 'maria@exemplo', 'maria silva@exemplo.com']) {
      it(`recusa o e-mail "${email}"`, () => {
        preencher('nomeCompleto', 'Maria da Silva');
        preencher('email', email);

        enviar();

        expect(enviados).toEqual([]);
        expect(erroDoCampo('email')).toBe('Informe um e-mail válido.');
      });
    }

    for (const telefone of ['119876543', '(11) 9876-543', '119876543210', '+55 11 98765-4321', '11 98765-432a']) {
      it(`recusa o telefone "${telefone}"`, () => {
        preencherValido();
        preencher('telefone', telefone);

        enviar();

        expect(enviados).toEqual([]);
        expect(erroDoCampo('telefone')).toBe('Informe o telefone com DDD, com 10 ou 11 dígitos.');
      });
    }

    for (const telefone of ['(11) 98765-4321', '11.3333.4444', '1133334444']) {
      it(`aceita o telefone "${telefone}"`, () => {
        preencherValido();
        preencher('telefone', telefone);

        enviar();

        expect(enviados.length).toBe(1);
      });
    }

    it('recusa texto acima do tamanho do campo', () => {
      preencherValido();
      preencher('areaOuCargoDeInteresse', 'a'.repeat(121));

      enviar();

      expect(enviados).toEqual([]);
      expect(erroDoCampo('areaOuCargoDeInteresse')).toBe('Use no máximo 120 caracteres.');
    });
  });

  describe('erros devolvidos pela API', () => {
    it('mostra o e-mail já cadastrado no campo de e-mail, e não em alerta genérico', () => {
      preencherValido();

      enviar();
      falhar({ tipo: 'campos', erros: { email: 'Já existe um candidato cadastrado com este e-mail.' } });

      expect(erroDoCampo('email')).toBe('Já existe um candidato cadastrado com este e-mail.');
      expect(tela.querySelector('[role="alert"]')).toBeNull();
      expect(botaoSalvar().disabled).toBeFalse();
    });

    it('mostra cada erro de validação da API junto ao seu campo', () => {
      preencherValido();

      enviar();
      falhar({ tipo: 'campos', erros: { nomeCompleto: 'Mensagem da API para o nome.' } });

      expect(erroDoCampo('nomeCompleto')).toBe('Mensagem da API para o nome.');
    });

    it('apaga o erro da API quando o campo é corrigido', () => {
      preencherValido();
      enviar();
      falhar({ tipo: 'campos', erros: { email: 'Já existe um candidato cadastrado com este e-mail.' } });
      expect(erroDoCampo('email')).not.toBe('');

      preencher('email', 'outra@exemplo.com');
      fixture.detectChanges();

      expect(erroDoCampo('email')).toBe('');
    });

    it('com erro em campo que o formulário não tem, avisa de forma genérica', () => {
      preencherValido();

      enviar();
      falhar({ tipo: 'campos', erros: { $: 'JSON inválido.' } });

      expect(tela.querySelector('[role="alert"]')?.textContent).toContain('Não foi possível salvar');
    });

    it('com o servidor indisponível, explica que não conseguiu falar com ele', () => {
      preencherValido();

      enviar();
      falhar({ tipo: 'servidorIndisponivel' });

      expect(tela.querySelector('[role="alert"]')?.textContent).toContain('Não foi possível falar com o servidor');
    });
  });

  describe('preenchimento a partir de um currículo em PDF', () => {
    const curriculo = new File(['%PDF-1.7'], 'curriculo.pdf', { type: 'application/pdf' });

    function escolherArquivo(arquivo: File): void {
      const seletor = tela.querySelector<HTMLInputElement>('input[type="file"]')!;
      const transferencia = new DataTransfer();
      transferencia.items.add(arquivo);
      seletor.files = transferencia.files;
      seletor.dispatchEvent(new Event('change'));
      fixture.detectChanges();
    }

    function extrair(campos: CamposExtraidos): void {
      respostaDaExtracao.next(campos);
      respostaDaExtracao.complete();
      fixture.detectChanges();
    }

    function valorDoCampo(campo: string): string {
      return tela.querySelector<HTMLInputElement>(`[formControlName="${campo}"]`)!.value;
    }

    it('envia o arquivo escolhido para a extração', () => {
      escolherArquivo(curriculo);

      expect(arquivosEnviados).toEqual([curriculo]);
    });

    it('mostra que está lendo o arquivo e impede outro envio enquanto isso', () => {
      escolherArquivo(curriculo);

      const botao = tela.querySelector<HTMLButtonElement>('.extracao button')!;
      expect(botao.disabled).toBeTrue();
      expect(botao.textContent).toContain('Lendo currículo');

      extrair({ nomeCompleto: null, email: null, telefone: null });

      expect(botao.disabled).toBeFalse();
    });

    it('preenche os campos identificados, que continuam editáveis, e o cadastro conclui', () => {
      escolherArquivo(curriculo);
      extrair({ nomeCompleto: 'Maria Aparecida da Silva', email: 'maria.silva@exemplo.com', telefone: '11987654321' });

      expect(valorDoCampo('nomeCompleto')).toBe('Maria Aparecida da Silva');
      expect(valorDoCampo('email')).toBe('maria.silva@exemplo.com');
      expect(valorDoCampo('telefone')).toBe('11987654321');

      preencher('nomeCompleto', 'Maria Aparecida da Silva Souza');
      preencher('areaOuCargoDeInteresse', 'Desenvolvimento de software');
      enviar();

      expect(enviados).toEqual([{
        nomeCompleto: 'Maria Aparecida da Silva Souza',
        email: 'maria.silva@exemplo.com',
        telefone: '11987654321',
        areaOuCargoDeInteresse: 'Desenvolvimento de software',
        resumoProfissional: null
      }]);
    });

    it('deixa vazio o campo não identificado, mesmo que já tivesse valor', () => {
      preencher('telefone', '(21) 3333-4444');

      escolherArquivo(curriculo);
      extrair({ nomeCompleto: 'Maria Aparecida da Silva', email: 'maria.silva@exemplo.com', telefone: null });

      expect(valorDoCampo('telefone')).toBe('');
    });

    it('com arquivo recusado pela API, mostra o motivo e mantém o formulário utilizável', () => {
      escolherArquivo(curriculo);
      respostaDaExtracao.error({ tipo: 'arquivoInvalido', mensagem: 'O arquivo enviado não é um PDF.' });
      fixture.detectChanges();

      expect(tela.querySelector('.extracao [role="alert"]')?.textContent).toContain('O arquivo enviado não é um PDF.');

      preencherValido();
      enviar();
      expect(enviados.length).toBe(1);
    });

    it('recusa arquivo acima de 5 MB sem enviá-lo', () => {
      escolherArquivo(new File([new Uint8Array(5 * 1024 * 1024 + 1)], 'grande.pdf', { type: 'application/pdf' }));

      expect(arquivosEnviados).toEqual([]);
      expect(tela.querySelector('.extracao [role="alert"]')?.textContent).toContain('O arquivo excede o limite de 5 MB.');
    });
  });
});
