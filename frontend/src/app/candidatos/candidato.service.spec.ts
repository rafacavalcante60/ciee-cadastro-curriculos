import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CandidatoService } from './candidato.service';
import { Candidato, NovoCandidato } from './candidato.model';

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

  it('lista candidatos com GET em /api/candidatos, na ordem devolvida pela API', () => {
    const outro: Candidato = { ...candidatoGravado, id: 3, nomeCompleto: 'João Pereira', email: 'joao@exemplo.com' };
    let recebidos: Candidato[] | undefined;

    servico.listar().subscribe(candidatos => (recebidos = candidatos));

    const requisicao = http.expectOne('/api/candidatos');
    expect(requisicao.request.method).toBe('GET');
    requisicao.flush([candidatoGravado, outro]);

    expect(recebidos).toEqual([candidatoGravado, outro]);
  });
});
