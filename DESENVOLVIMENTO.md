# Registro do desenvolvimento

Como organizei o trabalho, que decisões tomei e como usei IA. Escrito ao fim
de cada fatia, não reconstruído no final.

## Resumo

Usei o Claude Code (Anthropic), modelo Claude Opus 5, como par de programação
em todo o desafio. Primeiro o modelo me questionou sobre o desenho (27
perguntas em quatro rodadas); depois escrevi a especificação e o recorte em
tickets, e só então implementei, um ticket por vez. Não pedi o projeto pronto.

Toda fatia passa por duas revisões antes de fechar, uma contra os padrões do
projeto e outra contra o ticket. A verificação combina testes de integração
contra SQL Server real e conferência manual com a aplicação rodando.

## Onde discordei do modelo

- **E-mail único.** O modelo recomendou não restringir, porque não foi pedido.
  Cadastro duplicado é um problema real de recrutamento, então ficou com
  validação na aplicação e índice único no banco.
- **Telefone.** O modelo recomendou texto livre, para um telefone estranho
  vindo do PDF não bloquear o cadastro. Preferi dez ou onze dígitos após
  normalizar, com o campo opcional.

## Organização

A especificação está na issue
[#1](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/1), e
cada fatia vertical virou um ticket com critérios de aceite e dependências
([#2 a #9](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues)).
A #9, subir tudo com `docker compose`, entrou depois do esqueleto, para a
avaliadora não precisar instalar .NET, Node, `dotnet-ef` nem as bibliotecas do
Chrome. Considerei o [spec-kit](https://github.com/github/spec-kit), mas para
oito tickets as issues do GitHub fazem o mesmo papel com menos cerimônia.

Commits pequenos, em português, no padrão Conventional Commits, nenhum com
build quebrado ou teste vermelho.

## Fatia 1: esqueleto e conexão com o banco

[#2](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/2) ·
cerca de 2 horas, incluindo questionamento, especificação e tickets.

Monorepo com `frontend/` e `backend/`, SQL Server em container, testes nos dois
lados e um endpoint de saúde. A IA gerou o esqueleto, a fixture de
Testcontainers, o compose e o README.

- O endpoint de saúde confundia container desligado com banco não criado.
  Agora relata os dois separadamente e só devolve 503 quando o servidor não
  responde.
- O Chrome dos testes de frontend vem do puppeteer. Na versão 25,
  `executablePath()` virou assíncrono e passei a usar `computeExecutablePath`;
  o download falhava sem `unzip` e resolvi com `yauzl`.
- Nenhuma senha versionada: a connection string vem de variável de ambiente
  ou `dotnet user-secrets`.
- Achados da revisão: senha real no README, connection string malformada
  virando 500 em vez de 503 (corrigido, com teste) e `provideAnimationsAsync()`
  ausente. Rejeitei trocar os estados da saúde por enum: quatro valores num
  endpoint de diagnóstico não pagam um conversor de serialização.

## Fatia 2: cadastro manual e listagem

[#3](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/3) ·
cerca de 1 hora.

Entidade `Candidato`, migration com índice único em `Email`, `schema.sql`,
`POST` e `GET /api/candidatos`, formulário e listagem.

- **Relógio injetável (`TimeProvider`).** O teste de ordenação grava
  candidatos fora da ordem cronológica, e só passa se a listagem ordenar pela
  data, não pelo `Id`.
- **Data em UTC.** `datetime2` não guarda fuso; sem o conversor, a tela
  mostraria a hora três horas adiantada.
- **Rotas carregadas sob demanda.** O pacote inicial passou do limite do
  Angular com o Material (536 kB); com `loadComponent`, caiu para 327 kB.
- **`schema.sql` idempotente**, para o compose da #9 poder reaplicá-lo.
- Corrigi um build quebrado vindo da fatia anterior (`@angular/animations` não
  instalado) e um comando do README que falhava porque o `dotnet ef` grava o
  `schema.sql` com BOM.
- Na revisão, a normalização do e-mail saiu do controller para
  `NovoCandidato`. Rejeitei um tipo base comum para entidade, entrada e
  resposta, e um tipo `Email` próprio: seriam camadas sem retorno.
- Para conferir que os testes pegam erro, quebrei a ordenação, a normalização
  e a guarda de envio duplo de propósito; os testes falharam e restaurei.

## Fatia 3: detalhes do candidato

[#4](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/4) ·
cerca de 30 minutos.

`GET /api/candidatos/{id}` e a tela de detalhes, aberta pelo nome na listagem.

- O 404 sai como ProblemDetails pelo próprio `[ApiController]`; o teste confere
  o tipo de conteúdo para isso não regredir.
- A tela trata id que não é inteiro positivo como "não encontrado", sem chamar
  a API, e só o 404 mostra essa mensagem; servidor fora do ar mostra outra.
- O link fica no nome, não na linha inteira, porque uma linha clicável não é
  alcançável pelo teclado.
- A revisão apontou que eu tinha escrito testes da tela de detalhes, que a
  especificação exclui de propósito. Removi.

## Fatia 4: validações e mensagens de erro

[#5](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/5) ·
cerca de 20 minutos.

Regras de nome, e-mail, telefone e tamanho na API e no formulário, mensagens em
português junto a cada campo, 409 para e-mail repetido e aviso de servidor
indisponível.

- **DataAnnotations**, nativas e já convertidas em `ValidationProblemDetails`
  pelo `[ApiController]`. FluentValidation seria mais uma biblioteca para cinco
  campos.
- **Erros com o nome do campo no JSON** (`nomeCompleto`), para o formulário
  associar cada erro ao seu campo sem tradução.
- **Mesma regra nos dois lados**, com a mesma expressão de e-mail e a mesma
  normalização de telefone, que grava só dígitos.
- **E-mail único em dois níveis:** checagem antes do insert e captura da
  violação do índice (erros 2601 e 2627), para a corrida entre dois cadastros.
  Dez cadastros simultâneos com o mesmo e-mail dão um 201 e nove 409.
- O título do erro de validação continuava em inglês; percebi ao chamar a API
  à mão e traduzi.
- Na revisão, o nome `" M "` passava pelo mínimo de 2 caracteres; agora os
  espaços das pontas não contam. Rejeitei traduzir os erros de JSON
  malformado, que o formulário nunca envia.

## Fatia 5: importação de currículo em PDF

[#6](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/6) ·
cerca de 1 hora.

`POST /api/curriculos/extracao` recebe um PDF e devolve nome, e-mail e
telefone, com `null` no que não encontrou. Na tela, um botão acima do
formulário preenche os campos, que continuam editáveis; o salvamento segue
sendo o mesmo `POST` de JSON.

- **Extração como função de texto para campos**, sem HTTP nem PDF, o que
  permite 41 casos de teste sem gerar um PDF por caso. Não criei interface C#,
  porque há uma única implementação.
- **Heurísticas:** e-mail e telefone por expressão regular, com o `+55`
  removido e números depois de CPF, CNPJ, CEP ou RG descartados; nome pelo
  rótulo `Nome:` ou pela primeira linha de palavras capitalizadas, com nome
  todo em maiúsculas convertido para iniciais maiúsculas.
- **Tipo do arquivo pela assinatura `%PDF-`**, não pela extensão nem pelo
  content-type, que o cliente declara como quiser.
- **O PDF não vai para o disco.** O ASP.NET grava em arquivo temporário todo
  upload acima de 64 KB; o endpoint eleva esse limite para manter o arquivo em
  memória. Confirmei apontando a pasta temporária para um diretório sem
  permissão de escrita: sem o ajuste, o upload falhou.
- **Amostras em `samples/`**, geradas com QuestPDF e iguais byte a byte a cada
  geração. Um teste envia cada PDF da pasta e compara com `esperado.json`.
- Eu tinha comentado que a leitura do PdfPig embaralhava as colunas sem ter
  testado. Ao testar, o problema era outro (o texto vinha sem quebras de
  linha), e corrigi o comentário.
- A revisão achou dois defeitos, corrigidos com teste: nome com hífen virava
  "Maria-josé", e "RG — Tel: 11 98765-4321" perdia o DDD do telefone.
  Rejeitei concentrar o limite de 5 MB num lugar só: ele fica na API e no
  formulário, como as demais regras.

## Limitações conhecidas

<!-- Consolidado na fatia de documentação, após a importação de PDF estar pronta. -->

## Melhorias com mais tempo

<!-- Consolidado na fatia de documentação. -->
