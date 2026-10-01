# Registro do desenvolvimento

Como o trabalho foi organizado, que decisões tomei e como usei IA. Atualizado
ao fim de cada fatia de trabalho, não reconstruído no final.

## Resumo

- **Ferramenta:** Claude Code (Anthropic), modelo Claude Opus 5, como par de
  programação em todo o desafio.
- **Fluxo:** primeiro o modelo me questionou sobre o desenho (27 perguntas em
  quatro rodadas), depois escrevi a especificação e o recorte em tickets, e
  só então implementei, um ticket por vez. Não pedi o projeto pronto.
- **Discordei do modelo** em duas decisões de produto (abaixo).
- **Toda fatia passa por revisão** antes de fechar: uma conferindo os padrões
  do projeto e outra conferindo o ticket. Os achados e o que fiz com eles estão
  registrados em cada fatia.
- **Verificação:** testes de integração contra SQL Server real e conferência
  manual com a aplicação rodando.

## Decisões em que discordei do modelo

- **E-mail único.** O modelo recomendou não restringir, porque não foi pedido.
  Discordei: cadastro duplicado é um problema real de recrutamento, e uma
  mensagem de erro específica ajuda mais do que nenhuma verificação. Ficou com
  validação na aplicação e índice único no banco.
- **Formato de telefone.** O modelo recomendou texto livre, para que um telefone
  estranho vindo de um PDF não bloqueasse o cadastro. Preferi um controle
  simples: dez ou onze dígitos após normalização, com o campo opcional.

## Organização

1. **Questionamento do desenho:** escopo, infraestrutura, modelo de dados,
   contrato da API, testes, documentação e critério de parada, cada pergunta
   com recomendação e motivo.
2. **Especificação** publicada como issue:
   [#1](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/1).
3. **Tickets**, cada um uma fatia vertical verificável, com dependências
   declaradas: [#2 a #9](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues).
   O recorte inicial tinha sete (#2 a #8). O oitavo, #9, entrou depois de
   fechado o esqueleto: subir a aplicação inteira com `docker compose`, para
   que a avaliadora não precise instalar .NET, Node, `dotnet-ef` nem as
   bibliotecas do Chrome.

**Alternativa considerada:** o [spec-kit](https://github.com/github/spec-kit)
do GitHub, que gera especificação, plano e lista de tarefas como arquivos
versionados para cada funcionalidade. Esse formato compensa em projetos maiores
ou com várias pessoas. Aqui o escopo cabe em oito tickets, e issues no GitHub
cumprem o mesmo papel com menos cerimônia: cada uma já traz critério de aceite
e dependências, e a revisão de cada fatia confere o código contra o ticket.

Commits pequenos, em português, seguindo Conventional Commits. Nenhum entra com
compilação quebrada ou teste vermelho.

## Fatia 1: esqueleto e conexão com o banco

Ticket: [#2](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/2).

Monorepo com `frontend/` e `backend/`, SQL Server em container, testes
configurados nos dois lados e um endpoint de saúde. Nenhum comportamento de
produto ainda. **Tempo:** cerca de 2 horas, incluindo questionamento,
especificação e tickets.

**Onde a IA ajudou:** esqueleto dos dois projetos, fixture de Testcontainers,
compose e README.

**O que corrigi ou adaptei:**

- **Endpoint de saúde redesenhado.** A primeira versão só dizia se o banco
  estava acessível, o que confundia container desligado com banco ainda não
  criado. Agora relata os dois separadamente e só devolve 503 quando o servidor
  não responde.
- **Chrome dos testes de frontend.** `puppeteer.executablePath()` virou
  assíncrono na versão 25; passei a usar `computeExecutablePath`. O download
  falhava sem mensagem por falta de `unzip` no sistema; resolvi com a
  dependência `yauzl`. `libnss3` e `libasound2t64` continuam necessárias e
  estão no README.
- **Sem senha versionada.** Credenciais vêm de variável de ambiente ou
  `dotnet user-secrets`, não do `appsettings`.

**Achados da revisão:**

- Senha real no README, contradizendo o próprio texto: trocada por `SUA_SENHA`.
  Mantive a senha no `.env.example` de propósito, porque um placeholder ali faz
  o container falhar num clone novo.
- Connection string malformada vazava como 500 (`ArgumentException`, não
  `SqlException`). Corrigido para 503, com teste de regressão.
- Faltava `provideAnimationsAsync()`, que o `ng add` não registrou. Adicionado
  junto com `provideHttpClient()`.
- README prometia `docs/adr/` e um passo de migration que ainda não tem efeito.
  Ajustado.
- Comentários em inglês herdados do scaffold, traduzidos.
- **Rejeitado:** transformar os campos de situação da saúde em enum. São quatro
  valores num endpoint de diagnóstico, e um conversor de serialização seria
  mais código do que o problema pede.

**Como verifiquei:** três testes de integração (servidor alcançável, banco
ausente, servidor fora do ar) e chamadas manuais a `/api/saude` e ao Swagger
com o container do compose.

## Fatia 2: cadastro manual e listagem

Ticket: [#3](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/3).

Primeira fatia que passa por todas as camadas: entidade `Candidato`, migration
inicial com índice único em `Email`, `schema.sql` exportado, `POST` e
`GET /api/candidatos`, formulário e listagem no Angular, e testes nos dois
lados. **Tempo:** cerca de 1 hora, incluindo revisão e conferência manual.

**Onde a IA ajudou:** implementação em TDD a partir dos critérios do ticket,
testes no seam HTTP e no `CandidatoService`, conferência do fluxo no navegador
com o Chrome do puppeteer, README.

**Decisões desta fatia:**

- **Relógio injetável (`TimeProvider`).** O teste de ordenação grava três
  candidatos fora da ordem cronológica, com o mais recente recebendo o menor
  `Id`. Só passa se a listagem ordenar pela data. Com o relógio do sistema, os
  cadastros sairiam em ordem crescente e uma ordenação por `Id` passaria igual.
- **`DataCadastro` marcada como UTC na leitura.** `datetime2` não guarda fuso.
  Sem o conversor, a API devolvia a data sem o `Z` e o navegador a mostraria
  três horas adiantada.
- **Banco limpo a cada teste.** Os testes compartilham um único container, e a
  listagem devolve tudo o que há na tabela. Apagar os candidatos antes de cada
  teste faz o resultado não depender da ordem de execução.
- **Telas carregadas sob demanda.** Com o Material, o pacote inicial passou do
  limite de aviso do Angular (536 kB contra 500 kB). Em vez de aumentar o
  limite, as rotas passaram a usar `loadComponent`, e o pacote inicial caiu
  para 327 kB.
- **`schema.sql` idempotente desde já**, gerado com `--idempotent`, para que o
  serviço de migração do compose (#9) possa reaplicá-lo sobre um volume
  existente.

**O que corrigi ou adaptei:**

- **Build quebrado vindo da fatia anterior.** O `provideAnimationsAsync()`
  registrado na revisão do esqueleto carrega `@angular/animations`, que não
  estava instalado. O `ng build` falhava, e nenhum teste pegava, porque nenhum
  componente do Material era renderizado ainda. Corrigido num commit próprio.
- **Comando do README que não funcionava.** A primeira versão do caminho sem
  `dotnet-ef` passava o `schema.sql` ao `sqlcmd` pela entrada padrão. Falhou
  ao testar, porque o `dotnet ef` grava o arquivo com BOM. O README agora copia
  o arquivo para o container e usa `-i`.
- **Teste do scaffold removido.** O `app.component.spec.ts` verificava o texto
  de boas-vindas do Angular, que saiu. O componente raiz não é um dos seams
  acordados, então não ganhou teste novo.

**Achados da revisão:**

- Normalização do e-mail estava solta no controller. Foi para
  `NovoCandidato.ParaCandidato`, onde a checagem de duplicidade da #5 também
  vai precisar dela.
- Leitura da listagem repetida nos testes: extraída para `ListarAsync()`.
- `opcional()` no formulário não dizia o que fazia: virou `nuloSeEmBranco()`.
- **Rejeitado:** um tipo comum para os campos repetidos em entidade, entrada e
  resposta. São três papéis distintos, e uma base compartilhada seria a camada
  extra que a especificação descarta.
- **Rejeitado:** um tipo `Email` próprio. Concentrar a normalização num lugar
  resolve o problema sem um tipo a mais para explicar.
- **Fica para a #5:** telefone ainda é gravado como digitado, sem normalizar
  para dígitos, e texto acima do tamanho da coluna estoura como 500 em vez de
  400. As duas coisas são validação, que este ticket deixou de fora.

**Como verifiquei:**

- Cinco testes de integração contra SQL Server real: o cadastro aparece na
  listagem, a ordenação é pela data, a lista vazia volta vazia, a data enviada
  pelo cliente é ignorada, e o e-mail é normalizado.
- Quatro testes do formulário e dois do `CandidatoService`.
- Para conferir que os testes detectam erro, alterei o código de propósito: troquei a ordenação por
  `Id`, removi a normalização e removi a guarda de envio duplicado. Os testes
  correspondentes falharam e depois restaurei o código.
- No navegador: lista vazia com mensagem, cadastro com duplo clique gerando um
  único `POST`, e-mail digitado como `  Maria.Silva@Exemplo.COM ` gravado em
  minúsculas, e hora exibida no fuso local.
- `schema.sql` aplicado duas vezes seguidas no mesmo banco, sem erro.

## Fatia 3: detalhes do candidato

Ticket: [#4](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/4).

`GET /api/candidatos/{id}` e a tela de detalhes, alcançada pelo nome do
candidato na listagem. Candidato inexistente gera 404 em ProblemDetails na API
e uma mensagem de "não encontrado" na tela. **Tempo:** cerca de 30 minutos,
incluindo revisão.

**Onde a IA ajudou:** testes no seam HTTP escritos antes do endpoint, tela de
detalhes com seus testes, conferência do fluxo no navegador.

**Decisões desta fatia:**

- **404 pelo `[ApiController]`.** `NotFound()` já sai como ProblemDetails, com
  `application/problem+json`. O controller não monta corpo de erro, e o teste
  confere o tipo de conteúdo para que isso não regrida em silêncio.
- **Identificador inválido tratado na tela.** A rota da API exige inteiro
  (`{id:int}`), então `/candidatos/abc` receberia um 404 sem corpo. A tela
  trata qualquer valor que não seja inteiro positivo como "não encontrado",
  sem chamar a API.
- **"Não encontrado" separado de falha.** Só o 404 mostra "Candidato não
  encontrado". Servidor fora do ar mostra a mensagem genérica de falha, para
  não dizer que o candidato não existe quando o problema é outro.
- **Resumo por inteiro com as quebras de linha.** O teste HTTP usa um resumo
  de cerca de 1.650 caracteres, em dois parágrafos, e confere que volta igual,
  quebras incluídas. Na tela, `white-space: pre-wrap` mantém os parágrafos.
- **Link no nome, não na linha inteira.** Uma linha clicável não é alcançável
  pelo teclado sem código extra; um link é.

**O que corrigi ou adaptei:**

- O primeiro teste de detalhes usava um resumo de 2.240 caracteres e falhou no
  `POST`, porque a coluna tem 2.000. Reduzi o texto. O estouro virar 500 em vez
  de 400 continua na pendência de validação registrada na fatia anterior.

**Achados da revisão:**

- **Teste de componente contra a especificação.** Escrevi cinco testes da tela
  de detalhes, mas a especificação (#1) exclui de propósito teste de componente
  de listagem ou de detalhes: os seams do frontend são o formulário e o
  `CandidatoService`. Removi o arquivo. O comportamento da tela fica coberto
  pela conferência no navegador.
- O registro dizia que o resumo voltava com as quebras de linha, mas nenhum
  teste usava quebra. O resumo do teste HTTP passou a ter dois parágrafos.
- Dois comentários repetiam o que o código já mostra (`pre-wrap` no SCSS e o
  ProblemDetails do `NotFound()`, já declarado no `ProducesResponseType`).
  Removidos.
- **Rejeitado:** extrair o corpo do `POST` repetido entre dois testes de
  integração. São dois usos, e o corpo por extenso deixa claro o que cada teste
  grava.
- **Rejeitado:** constante para o texto "Não informado", que aparece três vezes
  num único template.

**Como verifiquei:**

- Dois testes de integração novos contra SQL Server real: detalhes devolvem os
  dados gravados, e id inexistente devolve 404 em ProblemDetails, com
  `application/problem+json`. Os dois falharam antes do endpoint existir.
- Um teste novo no `CandidatoService` para o `GET` por id.
- No navegador: clique no nome na listagem abre os detalhes; campos ausentes
  aparecem como "Não informado"; `/candidatos/999999` e `/candidatos/abc`
  mostram "Candidato não encontrado".

## Limitações conhecidas

<!-- Consolidado na fatia de documentação, após a importação de PDF estar pronta. -->

## Melhorias com mais tempo

<!-- Consolidado na fatia de documentação. -->
