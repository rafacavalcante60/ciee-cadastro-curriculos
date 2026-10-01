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
