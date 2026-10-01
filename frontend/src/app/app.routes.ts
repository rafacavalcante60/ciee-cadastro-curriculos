import { Routes } from '@angular/router';

// Telas carregadas sob demanda: os módulos do Material de cada uma ficam fora
// do pacote inicial.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'candidatos' },
  {
    path: 'candidatos',
    title: 'Candidatos',
    loadComponent: () =>
      import('./candidatos/lista-candidatos/lista-candidatos.component').then(m => m.ListaCandidatosComponent)
  },
  {
    path: 'candidatos/novo',
    title: 'Novo candidato',
    loadComponent: () =>
      import('./candidatos/cadastro-candidato/cadastro-candidato.component').then(m => m.CadastroCandidatoComponent)
  },
  { path: '**', redirectTo: 'candidatos' }
];
