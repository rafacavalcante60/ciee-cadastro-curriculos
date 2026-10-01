export interface Candidato {
  id: number;
  nomeCompleto: string;
  email: string;
  telefone: string | null;
  areaOuCargoDeInteresse: string | null;
  resumoProfissional: string | null;
  /** ISO 8601 em UTC. */
  dataCadastro: string;
}

export interface NovoCandidato {
  nomeCompleto: string;
  email: string;
  telefone: string | null;
  areaOuCargoDeInteresse: string | null;
  resumoProfissional: string | null;
}

export type FalhaAoCadastrar =
  | { tipo: 'campos'; erros: Record<string, string> }
  | { tipo: 'servidorIndisponivel' }
  | { tipo: 'inesperada' };

/** Palpite da extração: null no campo que não foi identificado. */
export interface CamposExtraidos {
  nomeCompleto: string | null;
  email: string | null;
  telefone: string | null;
}

export type FalhaNaExtracao =
  | { tipo: 'arquivoInvalido'; mensagem: string }
  | { tipo: 'servidorIndisponivel' }
  | { tipo: 'inesperada' };
