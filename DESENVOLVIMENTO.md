# Registro do desenvolvimento

Como organizei o trabalho, que decisões tomei e como usei IA. Escrito ao fim
de cada fatia, não reconstruído no final.

## Resumo

Cerca de 6 horas no total, contando o questionamento inicial, a especificação
e os tickets.

Usei o Claude Code (Anthropic), com os modelos Claude Opus 5 e Opus 5.5, como
par de programação em todo o desafio, mas não pedi o projeto pronto. Primeiro
pedi ao modelo que me questionasse sobre o desenho (27 perguntas em quatro
rodadas). Das respostas saíram a especificação e os tickets, e só então
implementei, um ticket por vez. A última fatia, por exemplo, foi pedida com
`/implement issue #8`, um comando do Claude Code que lê o ticket, implementa
com testes primeiro onde cabe, roda as revisões e faz os commits.

Cada fatia passa por duas revisões feitas por agentes separados, uma contra os
padrões do projeto e outra contra o ticket, e depois por testes de integração
contra SQL Server real e conferência manual com a aplicação rodando.

A configuração do assistente (o `CLAUDE.md` e as instruções em `docs/agents/`)
ficou no `.gitignore` porque é ambiente meu, não do projeto. O uso de IA está
descrito aqui.

Ao longo do caminho pedi ao modelo que parasse de atualizar o README a cada
fatia, porque ninguém o lê no meio do caminho; que encurtasse este registro,
que chegou a 464 linhas depois de cinco fatias; que não adicionasse lint, que
nenhum ticket pedia; e que apagasse o glossário.

## Onde discordei do modelo

- E-mail único: o modelo recomendou não restringir, porque não foi pedido.
  Cadastro duplicado é um problema real de recrutamento, então ficou com
  validação na aplicação e índice único no banco.
- Telefone: o modelo recomendou texto livre, para um telefone estranho vindo
  do PDF não bloquear o cadastro. Preferi dez ou onze dígitos depois de
  normalizar, com o campo opcional.

## Erros da IA que precisei corrigir

- Registrou o provedor de animações sem instalar o pacote, quebrando o build
  por onze commits (fatia 1).
- Escreveu testes da tela de detalhes que a especificação excluía (fatia 3).
- Afirmou num comentário que o PdfPig embaralhava colunas, sem ter testado; o
  problema real era a falta de quebras de linha (fatia 5).
- Implementou um aviso único onde a especificação pedia uma lista, e as duas
  revisões automáticas não perceberam; só apareceu no teste à mão (fatia 6).
- Pôs uma linha de coautoria no commit `e72021c`, contra a regra que eu tinha
  dado (entre as fatias 2 e 3).
- Afirmou neste registro que nenhum commit tinha build quebrado (fatia 8).

## Organização

A especificação está na issue
[#1](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/1), e
cada fatia vertical virou um ticket com critérios de aceite e dependências
([#2 a #9](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues)).
A #9, subir tudo com `docker compose`, entrou depois do esqueleto, para a
avaliadora não precisar instalar .NET, Node, `dotnet-ef` nem as bibliotecas do
Chrome. Considerei o [spec-kit](https://github.com/github/spec-kit), mas para
oito tickets as issues do GitHub bastam. Também escrevi um glossário e depois
o apaguei: com uma entidade só, os termos se explicam.

Commits pequenos, em português, no padrão Conventional Commits. Onze commits
da fatia 1, de `d607a30` a `078ad87`, não compilam o frontend: o provedor de
animações foi registrado sem instalar `@angular/animations`, e só `3ab3c40`
corrige (conferi fazendo o build em `d607a30`). Não reescrevi o histórico, que
já estava publicado.

## Fatia 1: esqueleto e conexão com o banco

[#2](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/2) ·
cerca de 1 hora e 20 minutos, incluindo questionamento, especificação e
tickets, numa janela de 3 horas com pausas.

Monorepo com `frontend/` e `backend/`, SQL Server em container, testes nos dois
lados e um endpoint de saúde. A IA gerou o esqueleto, a fixture de
Testcontainers, o compose e o README. Nenhuma senha é versionada: a connection
string vem de variável de ambiente ou de `dotnet user-secrets`.

- O endpoint de saúde confundia container desligado com banco não criado.
  Agora relata os dois separadamente e só devolve 503 quando o servidor não
  responde.
- O Chrome dos testes de frontend vem do puppeteer, que na versão 25 tornou
  `executablePath()` assíncrono (passei a usar `computeExecutablePath`) e não
  baixava sem `unzip` (resolvi com `yauzl`). Isso levou mais tempo que o
  próprio teste.
- A revisão achou senha real no README, connection string malformada virando
  500 em vez de 503 (corrigido, com teste) e `provideAnimationsAsync()`
  ausente. Rejeitei trocar os estados da saúde por enum: quatro valores num
  endpoint de diagnóstico não pagam um conversor de serialização.

## Fatia 2: cadastro manual e listagem

[#3](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/3) ·
cerca de 1 hora.

Entidade `Candidato`, migration com índice único em `Email`, `schema.sql`
idempotente (para o compose da #9 poder reaplicá-lo), `POST` e
`GET /api/candidatos`, formulário e listagem.

- `TimeProvider` injetável: o teste de ordenação grava candidatos fora da
  ordem cronológica e só passa se a listagem ordenar pela data, não pelo `Id`.
- Data em UTC: `datetime2` não guarda fuso, e sem o conversor a tela mostraria
  a hora três horas adiantada.
- Rotas carregadas sob demanda com `loadComponent`: com o Material, o pacote
  inicial passava do limite do Angular (536 kB) e caiu para 327 kB.
- Corrigi o build quebrado da fatia anterior e um comando do README que
  falhava porque o `dotnet ef` grava o `schema.sql` com BOM.
- Na revisão, a normalização do e-mail saiu do controller para
  `NovoCandidato`. Rejeitei um tipo base comum para entidade, entrada e
  resposta, e um tipo `Email` próprio: seriam camadas sem retorno.
- Para conferir que os testes pegam erro, quebrei de propósito a ordenação, a
  normalização e a guarda de envio duplo. Os testes falharam e restaurei.

## Fatia 3: detalhes do candidato

[#4](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/4) ·
cerca de 30 minutos.

`GET /api/candidatos/{id}` e a tela de detalhes, aberta pelo nome na listagem.

- O 404 sai como ProblemDetails pelo próprio `[ApiController]`, e o teste
  confere o tipo de conteúdo para isso não regredir.
- Id que não é inteiro positivo aparece como "não encontrado" sem chamar a
  API. Servidor fora do ar mostra outra mensagem.
- O link fica no nome, não na linha inteira, porque uma linha clicável não é
  alcançável pelo teclado.
- A revisão apontou testes da tela de detalhes, que a especificação exclui de
  propósito. Removi.

## Fatia 4: validações e mensagens de erro

[#5](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/5) ·
cerca de 20 minutos.

Regras de nome, e-mail, telefone e tamanho na API e no formulário, mensagens em
português junto a cada campo, 409 para e-mail repetido e aviso de servidor
indisponível.

- DataAnnotations, porque são nativas e o `[ApiController]` já as converte em
  `ValidationProblemDetails`. FluentValidation seria mais uma biblioteca para
  cinco campos.
- Os erros trazem o nome do campo como no JSON (`nomeCompleto`), e o formulário
  associa cada um ao seu campo sem tradução. A expressão de e-mail e a
  normalização de telefone (que grava só dígitos) são as mesmas nos dois lados.
- E-mail único em dois níveis: checagem antes do insert e captura da violação
  do índice (erros 2601 e 2627) para a corrida entre dois cadastros. Dez
  cadastros simultâneos com o mesmo e-mail dão um 201 e nove 409.
- O título do erro de validação continuava em inglês. Percebi ao chamar a API
  à mão e traduzi.
- Na revisão, o nome `" M "` passava pelo mínimo de 2 caracteres; agora os
  espaços das pontas não contam. Rejeitei traduzir os erros de JSON
  malformado, que o formulário nunca envia.

## Fatia 5: importação de currículo em PDF

[#6](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/6) ·
cerca de 1 hora.

`POST /api/curriculos/extracao` recebe um PDF e devolve nome, e-mail e
telefone, com `null` no que não encontrou. Na tela, um botão acima do
formulário preenche os campos, que continuam editáveis, e o salvamento segue
sendo o mesmo `POST` de JSON.

- A extração é uma função de texto para campos, sem HTTP nem PDF, o que permite
  41 casos de teste sem gerar um PDF por caso. Não criei interface C# para uma
  única implementação.
- E-mail e telefone saem por expressão regular, sem o `+55` e descartando
  números depois de CPF, CNPJ, CEP ou RG. O nome sai do rótulo `Nome:` ou da
  primeira linha de palavras capitalizadas; nome todo em maiúsculas vira
  iniciais maiúsculas.
- O tipo do arquivo é decidido pela assinatura `%PDF-`, não pela extensão nem
  pelo content-type, que o cliente declara como quiser.
- O PDF não vai para o disco. O ASP.NET grava em arquivo temporário todo upload
  acima de 64 KB, e o endpoint eleva esse limite. Confirmei apontando a pasta
  temporária para um diretório sem permissão de escrita: sem o ajuste, o
  upload falhou.
- As amostras em `samples/` são geradas com QuestPDF, iguais byte a byte a cada
  geração, e um teste compara cada uma com `esperado.json`.
- O texto da página no PdfPig vem sem quebras de linha, e a heurística de nome
  trabalha linha a linha; troquei pelo extrator que preserva as linhas. Antes
  de testar, eu tinha comentado que o problema era o embaralhamento de colunas,
  e corrigi o comentário.
- A revisão achou dois defeitos, corrigidos com teste: nome com hífen virava
  "Maria-josé", e "RG — Tel: 11 98765-4321" perdia o DDD. Rejeitei concentrar o
  limite de 5 MB num lugar só: ele fica na API e no formulário, como as demais
  regras.

## Fatia 6: bordas da importação de currículo

[#7](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/7) ·
cerca de 40 minutos.

PDF digitalizado, protegido por senha ou corrompido devolve 200 com os campos
`null` e um aviso. A tela mostra os avisos como informação, e o cadastro manual
segue normalmente.

- Falha de leitura é 200, não 4xx: o arquivo é um PDF, então a culpa não é do
  cliente, e um erro faria a tela bloquear o que não impede o cadastro.
  Arquivo que não é PDF continua 400.
- Texto vazio indica PDF digitalizado, a exceção de criptografia do PdfPig
  indica senha, e qualquer outra exceção vira "pode estar corrompido" e vai
  para o log. Capturo `Exception` porque o PdfPig não tem um tipo único para
  arquivo malformado.
- Os avisos vêm prontos da API, em português, e na tela usam `role="status"` e
  cor neutra, não `role="alert"` em vermelho.
- Eu tinha implementado um campo `aviso` único, e as duas revisões não
  perceberam que a spec pedia uma coleção. Notei ao testar à mão. Agora há uma
  lista com um aviso por campo não encontrado; quando a leitura falha, vai um
  aviso só.
- As amostras novas saem do mesmo currículo: convertido em imagem, criptografado
  e cortado no primeiro terço.
- A revisão achou um defeito, corrigido com teste: um PDF ilegível apagava o
  que já tinha sido digitado. Rejeitei levar a checagem de texto vazio para
  junto das outras falhas de leitura, porque o ticket pede o teste de unidade
  justamente sobre o texto.
- Cheguei a implementar a leitura das seções "Objetivo" e "Resumo", mas desfiz:
  o enunciado pede só nome, e-mail e telefone, e seria mais uma heurística
  frágil fora do pedido.

## Fatia 7: aplicação completa no Docker Compose

[#9](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/9) ·
cerca de 40 minutos.

`docker compose up --build` sobe banco, migração, API e frontend sem .NET, Node
nem `dotnet-ef` na máquina. `docker compose up -d banco` continua subindo só o
SQL Server, para o caminho com SDKs e para os testes.

- A migração é um serviço próprio, não um passo no start da API: roda o
  `schema.sql` uma vez e termina, e a API só sobe se ele terminar com sucesso.
  Reaproveita a imagem do SQL Server, que já traz o `sqlcmd`, e substituiu a
  receita manual do README (`docker compose run --rm migracao`).
- O script é gerado com `dotnet ef migrations script --idempotent` (regenerei e
  saiu idêntico ao versionado), então subir de novo sobre o mesmo volume não
  falha.
- O Nginx repassa `/api/` para a API, e o frontend segue com caminhos relativos
  e sem CORS. O limite de corpo subiu para 6 MB; com o padrão de 1 MB, um PDF
  válido de 2 MB levaria 413 sem chegar à API.
- A imagem da API ganhou curl só para o healthcheck, porque a `aspnet` não traz
  nenhum cliente HTTP.
- Conferi num clone limpo com volume novo: os quatro fluxos, o upload de
  `samples/curriculo-completo.pdf`, um PDF de 5,5 MB recusado pela API e um de
  7 MB recusado pelo Nginx, e um segundo `up` sobre o volume existente.
- A revisão apontou que a mensagem de banco ausente em `/api/saude` só citava
  o `dotnet ef`; agora cita o serviço de migração. Rejeitei o healthcheck
  aceitar banco sem schema (a ordem do compose já garante o schema) e o Nginx
  guardar o IP da API até reiniciar (só afeta recriar a API sozinha).

## Fatia 8: documentação e integração contínua

[#8](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/8) ·
cerca de 30 minutos.

README revisto inteiro, workflow de CI, este registro consolidado e a
aplicação conferida num clone limpo pelos dois caminhos.

- O README ganhou versões das ferramentas, o que cada amostra exercita, as
  decisões técnicas e o fora de escopo com motivo. Apaguei o README em inglês
  que o Angular CLI gerou em `frontend/`.
- Não fiz os ADRs que o ticket pede. As três decisões que eles guardariam
  (PdfPig, heurística, projeto único) estão, com alternativas e motivo, em
  "Decisões técnicas" no README.
- O CI usa Testcontainers em vez de um SQL Server declarado no workflow: o
  runner do GitHub já tem Docker, então o teste roda igual na máquina e no CI.
- No clone limpo, pelos dois caminhos (compose com volume novo e SDKs), um
  roteiro no Chrome headless percorreu cadastro manual, cadastro com PDF,
  listagem e detalhes, incluindo erro de validação, e-mail repetido, PDF
  digitalizado que mantém o digitado e id inexistente.
- Ao conferir, vi que o telefone aparecia nos detalhes só com dígitos
  (`11912345678`). O banco continua guardando só os dígitos, e um pipe do
  Angular passou a formatar na exibição: `(11) 91234-5678`.
- A revisão apontou que o README dizia que todas as amostras passam pelo
  `esperado.json` (o `nao-e-pdf.pdf` tem teste à parte) e que este registro
  afirmava conferência à mão antes de ela acontecer; corrigi os dois.

## Como verifiquei

Medido em 1º de outubro de 2026, no WSL2. A extração e o tempo de resposta
vêm do commit `229f88c`; a contagem de testes já inclui a formatação do
telefone.

| Projeto | Testes |
|---|---|
| Backend (xUnit: extração por unidade, API por integração com SQL Server real) | 106 |
| Frontend (Karma e Jasmine: formulário, serviço HTTP e formatação do telefone) | 51 |

| Amostras de `samples/`, contra o `esperado.json` | Resultado |
|---|---|
| Currículo legível: nome, e-mail e telefone certos | 4 de 4 (no sem telefone, o campo voltou vazio, como devia) |
| PDF ilegível (digitalizado, senha, corrompido): nenhum campo e o aviso certo | 3 de 3 |
| Arquivo que não é PDF recusado com 400 | 1 de 1 |

Esses acertos não medem a extração em currículos reais: as amostras foram
escritas junto com as heurísticas e servem para pegar regressão.

`POST /api/curriculos/extracao`, medido com `curl` na API local (`dotnet run`),
20 chamadas por amostra depois de uma de aquecimento: mediana entre 2,5 ms
(corrompido) e 10,2 ms (`curriculo-completo.pdf`), máximo de 29 ms.
