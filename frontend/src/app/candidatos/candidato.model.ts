/** Candidato como a API o devolve. */
export interface Candidato {
  id: number;
  nomeCompleto: string;
  email: string;
  telefone: string | null;
  areaOuCargoDeInteresse: string | null;
  resumoProfissional: string | null;
  /** Instante do cadastro em UTC, no formato ISO 8601, gerado no servidor. */
  dataCadastro: string;
}

/** Dados enviados pelo formulário de cadastro. Id e data são gerados no servidor. */
export interface NovoCandidato {
  nomeCompleto: string;
  email: string;
  telefone: string | null;
  areaOuCargoDeInteresse: string | null;
  resumoProfissional: string | null;
}
