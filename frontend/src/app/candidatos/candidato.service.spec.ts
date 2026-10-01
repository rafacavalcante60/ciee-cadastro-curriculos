import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CandidatoService } from './candidato.service';
import { CamposExtraidos, Candidato, FalhaAoCadastrar, FalhaNaExtracao, NovoCandidato } from './candidato.model';

describe('CandidatoService', () => {
  let servico: CandidatoService;
  let http: HttpTestingController;

  const candidatoGravado: Candidato = {
    id: 7,
    nomeCompleto: 'Maria da Silva',
    email: 'maria.silva@exemplo.com',
    telefone: '11987654321',
    areaOuCargoDeInteresse: 'Desenvolvimento de software',
    resumoProfissional: null,
    dataCadastro: '2026-03-10T14:30:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    servico = TestBed.inject(CandidatoService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('cria candidato com POST de JSON em /api/candidatos e devolve o candidato gravado', () => {
    const novo: NovoCandidato = {
      nomeCompleto: 'Maria da Silva',
      email: 'maria.silva@exemplo.com',
      telefone: '11987654321',
      areaOuCargoDeInteresse: 'Desenvolvimento de software',
      resumoProfissional: null
    };
    let recebido: Candidato | undefined;

    servico.criar(novo).subscribe(candidato => (recebido = candidato));

    const requisicao = http.expectOne('/api/candidatos');
    expect(requisicao.request.method).toBe('POST');
    expect(requisicao.request.body).toEqual(novo);
    requisicao.flush(candidatoGravado, { status: 201, statusText: 'Created' });

    expect(recebido).toEqual(candidatoGravado);
  });

  describe('ao falhar no cadastro', () => {
    const novo: NovoCandidato = {
      nomeCompleto: 'Maria da Silva',
      email: 'maria.silva@exemplo.com',
      telefone: null,
      areaOuCargoDeInteresse: null,
      resumoProfissional: null
    };
    let falha: FalhaAoCadastrar | undefined;

    beforeEach(() => {
      falha = undefined;
      servico.criar(novo).subscribe({ error: erro => (falha = erro) });
    });

    it('traduz o 400 em erros por campo, com a primeira mensagem de cada um', () => {
      http.expectOne('/api/candidatos').flush({
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: {
          nomeCompleto: ['O nome completo deve ter ao menos 2 caracteres.'],
          telefone: ['Informe o telefone com DDD, com 10 ou 11 dígitos.', 'Outra mensagem.']
        }
      }, { status: 400, statusText: 'Bad Request' });

      expect(falha).toEqual({
        tipo: 'campos',
        erros: {
          nomeCompleto: 'O nome completo deve ter ao menos 2 caracteres.',
          telefone: 'Informe o telefone com DDD, com 10 ou 11 dígitos.'
        }
      });
    });

    it('traduz o 409 em erro no campo de e-mail, com a mensagem da API', () => {
      http.expectOne('/api/candidatos').flush({
        title: 'E-mail já cadastrado',
        status: 409,
        detail: 'Já existe um candidato cadastrado com este e-mail.'
      }, { status: 409, statusText: 'Conflict' });

      expect(falha).toEqual({
        tipo: 'campos',
        erros: { email: 'Já existe um candidato cadastrado com este e-mail.' }
      });
    });

    it('trata falha de rede como servidor indisponível', () => {
      http.expectOne('/api/candidatos').error(new ProgressEvent('error'));

      expect(falha).toEqual({ tipo: 'servidorIndisponivel' });
    });

    it('trata o 500 do proxy de desenvolvimento, com a API fora do ar, como servidor indisponível', () => {
      http.expectOne('/api/candidatos').flush('', { status: 500, statusText: 'Internal Server Error' });

      expect(falha).toEqual({ tipo: 'servidorIndisponivel' });
    });

    it('trata outras respostas de erro como falha inesperada', () => {
      http.expectOne('/api/candidatos').flush('', { status: 404, statusText: 'Not Found' });

      expect(falha).toEqual({ tipo: 'inesperada' });
    });
  });

  it('lista candidatos com GET em /api/candidatos, na ordem devolvida pela API', () => {
    const outro: Candidato = { ...candidatoGravado, id: 3, nomeCompleto: 'João Pereira', email: 'joao@exemplo.com' };
    let recebidos: Candidato[] | undefined;

    servico.listar().subscribe(candidatos => (recebidos = candidatos));

    const requisicao = http.expectOne('/api/candidatos');
    expect(requisicao.request.method).toBe('GET');
    requisicao.flush([candidatoGravado, outro]);

    expect(recebidos).toEqual([candidatoGravado, outro]);
  });

  it('detalha candidato com GET em /api/candidatos/{id}', () => {
    let recebido: Candidato | undefined;

    servico.detalhar(7).subscribe(candidato => (recebido = candidato));

    const requisicao = http.expectOne('/api/candidatos/7');
    expect(requisicao.request.method).toBe('GET');
    requisicao.flush(candidatoGravado);

    expect(recebido).toEqual(candidatoGravado);
  });

  describe('extração de currículo', () => {
    const arquivo = new File(['%PDF-1.7'], 'curriculo.pdf', { type: 'application/pdf' });

    it('envia o arquivo em multipart para /api/curriculos/extracao e devolve os campos', () => {
      const campos: CamposExtraidos = { nomeCompleto: 'Maria da Silva', email: null, telefone: '11987654321' };
      let recebidos: CamposExtraidos | undefined;

      servico.extrairCurriculo(arquivo).subscribe(resposta => (recebidos = resposta));

      const requisicao = http.expectOne('/api/curriculos/extracao');
      expect(requisicao.request.method).toBe('POST');
      expect((requisicao.request.body as FormData).get('arquivo')).toBe(arquivo);
      requisicao.flush(campos);

      expect(recebidos).toEqual(campos);
    });

    it('traduz 400 em arquivo inválido, com a mensagem da API', () => {
      let falha: FalhaNaExtracao | undefined;

      servico.extrairCurriculo(arquivo).subscribe({ error: erro => (falha = erro) });

      http.expectOne('/api/curriculos/extracao').flush(
        { title: 'Arquivo inválido', detail: 'O arquivo enviado não é um PDF.' },
        { status: 400, statusText: 'Bad Request' });

      expect(falha).toEqual({ tipo: 'arquivoInvalido', mensagem: 'O arquivo enviado não é um PDF.' });
    });

    it('traduz falha de rede em servidor indisponível', () => {
      let falha: FalhaNaExtracao | undefined;

      servico.extrairCurriculo(arquivo).subscribe({ error: erro => (falha = erro) });

      http.expectOne('/api/curriculos/extracao').error(new ProgressEvent('error'));

      expect(falha).toEqual({ tipo: 'servidorIndisponivel' });
    });
  });
});
