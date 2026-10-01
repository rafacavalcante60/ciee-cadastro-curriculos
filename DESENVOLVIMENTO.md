# Registro do desenvolvimento

Como organizei o trabalho, que decisões tomei e como usei IA. Escrito ao fim
de cada fatia, não reconstruído no final.

## Resumo

Usei o Claude Code (Anthropic), com os modelos Claude Opus 5 e Opus 5.5, como
par de programação em todo o desafio. Primeiro o modelo me questionou sobre o desenho (27
perguntas em quatro rodadas); depois escrevi a especificação e o recorte em
tickets, e só então implementei, um ticket por vez. Não pedi o projeto pronto.

A configuração do assistente (o `CLAUDE.md` com as regras do projeto e as
instruções em `docs/agents/`) ficou fora do repositório, no `.gitignore`: é
ambiente meu, não do projeto, e quem avalia não precisa dela. O uso de IA não
fica escondido por isso; está descrito aqui.

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

## Como usei a IA

- **Desenho:** pedi ao modelo que me questionasse sobre o plano antes de
  escrever código; das respostas saíram a especificação e os tickets.
- **Implementação:** um ticket por vez. O pedido desta última fatia foi
  `/implement issue #8`, um comando do Claude Code que lê o ticket, implementa
  com testes primeiro onde cabe, roda as duas revisões e faz os commits.
- **Revisão:** as duas revisões de cada fatia são agentes separados, um contra
  os padrões do projeto e outro contra o ticket. Os achados que rejeitei estão
  em cada fatia.
- **Correções de rumo que pedi:** parar de atualizar o README a cada fatia,
  porque ninguém o lê no meio do caminho; encurtar este registro, que chegou a
  464 linhas depois de cinco fatias; não adicionar lint, que nenhum ticket
  pedia; e apagar o glossário.

## Organização

A especificação está na issue
[#1](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/1), e
cada fatia vertical virou um ticket com critérios de aceite e dependências
([#2 a #9](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues)).
A #9, subir tudo com `docker compose`, entrou depois do esqueleto, para a
avaliadora não precisar instalar .NET, Node, `dotnet-ef` nem as bibliotecas do
Chrome. Considerei o [spec-kit](https://github.com/github/spec-kit), mas para
oito tickets as issues do GitHub fazem o mesmo papel com menos cerimônia.
Também escrevi um glossário do domínio e depois o apaguei: com uma entidade
só, os termos se explicam, e as decisões que ele guardava já estão no README
e neste registro.

Commits pequenos, em português, no padrão Conventional Commits. Um trecho de
onze commits da fatia 1, de `d607a30` a `078ad87`, não compila o frontend: o
provedor de animações foi registrado sem instalar `@angular/animations`, e só
`3ab3c40` corrige. Conferi fazendo o build em `d607a30`. Não reescrevi o
histórico, que já estava publicado.

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

## Fatia 6: bordas da importação de currículo

[#7](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/7) ·
cerca de 40 minutos.

PDF digitalizado, protegido por senha ou corrompido devolve 200 com os campos
`null` e um aviso; a tela mostra os avisos como informação, e o cadastro manual
segue normalmente.

- **Falha de leitura é 200, não 4xx.** O arquivo é um PDF, então a culpa não é
  do cliente, e um erro levaria a tela a tratar como bloqueante o que não
  impede o cadastro. Arquivo que não é PDF continua 400.
- **Um aviso por causa:** texto vazio indica PDF digitalizado; a exceção de
  criptografia do PdfPig indica senha; qualquer outra exceção vira "pode estar
  corrompido" e vai para o log. Capturo `Exception` porque o PdfPig não tem um
  tipo único para arquivo malformado.
- **Os avisos vêm prontos da API**, em português, como o `detail` dos erros. Na
  tela usam `role="status"` e cor neutra, e não `role="alert"` em vermelho.
- **Uma lista de avisos, não um texto só.** Eu tinha implementado um campo
  `aviso` único, e as duas revisões não perceberam que a spec pedia "uma
  coleção de avisos legíveis"; notei ao testar à mão. Agora a lista também
  diz qual campo não foi encontrado, um aviso por campo. Quando a leitura
  falha, vai um aviso só, para não repetir três vezes a mesma causa.
- **Amostras pelo mesmo gerador:** o digitalizado é a página do currículo
  completo convertida em imagem; o protegido é o mesmo currículo
  criptografado; o corrompido é o mesmo arquivo cortado no primeiro terço.
- Antes de escrever as limitações, rodei a extração em casos-limite em vez de
  supor o resultado. Um achado: `+44 20 7946 0958` vira o telefone
  `2079460958`. Ficou documentado, não corrigido.
- A revisão achou um defeito, corrigido com teste: um PDF ilegível apagava o
  que já tinha sido digitado. Agora, com aviso, o formulário fica como estava.
  Rejeitei levar a checagem de texto vazio para junto das outras falhas de
  leitura: o ticket pede o teste de unidade justamente sobre o texto.
- Ao testar à mão, estranhei que área de interesse e resumo não vinham do PDF.
  Cheguei a implementar a leitura das seções "Objetivo" e "Resumo", mas
  desfiz: o enunciado pede só nome, e-mail e telefone, e seria mais uma
  heurística frágil fora do pedido.

## Fatia 7: aplicação completa no Docker Compose

[#9](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/9) ·
cerca de 40 minutos.

`docker compose up --build` sobe banco, migração, API e frontend sem .NET, Node
nem `dotnet-ef` na máquina; `docker compose up -d banco` continua subindo só o
SQL Server para o caminho com SDKs e para os testes.

- **Migração num serviço próprio, e não no start da API.** Mantive a decisão da
  fatia 1: criar o schema é um passo visível, que roda o `schema.sql` uma vez e
  termina. A API só sobe se ele terminar com sucesso. O serviço reaproveita a
  imagem do SQL Server, que já traz o `sqlcmd`, e serve também ao caminho
  local: `docker compose run --rm migracao` substituiu a receita manual do
  README.
- **Script idempotente**, gerado com `dotnet ef migrations script --idempotent`
  (regenerei e saiu idêntico ao versionado). Subir de novo sobre o mesmo volume
  não falha.
- **Nginx no lugar do proxy do dev server:** repassa `/api/` para a API, então o
  frontend segue com caminhos relativos e sem CORS. O limite de corpo subiu
  para 6 MB; com o padrão de 1 MB, um PDF válido de 2 MB levaria 413 do Nginx
  sem chegar à API.
- **curl na imagem da API** só para o healthcheck em `/api/saude`; a imagem
  `aspnet` não traz nenhum cliente HTTP.
- **Verificação:** num clone limpo com volume novo, os quatro fluxos no Chrome
  headless e por `curl`, o upload de `samples/curriculo-completo.pdf`, um PDF
  de 5,5 MB recusado pela API (e não pelo Nginx) e um de 7 MB recusado pelo
  Nginx, e um segundo `up` sobre o volume existente.
- A revisão apontou que a mensagem de banco ausente em `/api/saude` só citava o
  `dotnet ef`; agora cita o serviço de migração. Rejeitei dois achados: o
  healthcheck aceitar banco sem schema (a ordem do compose já garante o
  schema) e o Nginx guardar o IP da API até reiniciar (só afeta recriar a API
  sozinha, fora do fluxo pedido).

## Fatia 8: documentação e integração contínua

[#8](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/8) ·
cerca de 1 hora.

README revisto inteiro, workflow de CI, este registro consolidado e a
aplicação conferida num clone limpo pelos dois caminhos.

- **README:** saiu a faixa de estado atual; entraram versões de todas as
  ferramentas, as amostras com o que cada uma exercita, as decisões técnicas e o
  fora de escopo com o motivo de cada item. O README do `frontend/`, gerado pelo
  Angular CLI em inglês, foi apagado.
- **ADRs não foram feitos**, embora o ticket peça, e o glossário foi apagado
  (ver Organização). As três decisões que os ADRs guardariam (PdfPig,
  heurística, projeto único) estão, com alternativas e motivo, na seção
  "Decisões técnicas" do README.
- **CI com Testcontainers**, e não com um SQL Server declarado como serviço do
  workflow: o runner do GitHub já tem Docker, então o teste roda igual na
  máquina e no CI, sem configuração própria.
- **Clone limpo** do GitHub nos dois caminhos: `docker compose up --build` com
  volume novo, e o caminho com SDKs (`dotnet ef database update`, `dotnet run`,
  `npm start`). Nos dois, um roteiro no Chrome headless percorreu cadastro
  manual, cadastro com PDF, listagem e detalhes, incluindo os desvios: erro de
  validação, e-mail repetido, PDF digitalizado que mantém o digitado e id
  inexistente.
- **Achado ao conferir:** o telefone aparece nos detalhes só com dígitos
  (`11912345678`). Não corrigi; ficou nas melhorias.
- **Erro no próprio registro:** a seção de organização dizia que nenhum commit
  tinha build quebrado, o que era falso. Corrigido, com os commits.
- A revisão apontou que o README dizia que todas as amostras passam pelo
  `esperado.json` (o `nao-e-pdf.pdf` tem teste à parte) e que o registro
  afirmava conferência à mão antes de ela acontecer; corrigi os dois. Rejeitei
  tirar de "Melhorias" o que já está em "Fora de escopo": um diz o que ficou de
  fora, o outro o que eu faria primeiro, para leitores diferentes.

## Como verifiquei

Medido em 1º de outubro de 2026, com o código do commit `229f88c` (a fatia 8
só mexe em documentação e CI), no WSL2 (Linux sobre Windows).

| Projeto | Testes |
|---|---|
| Backend (xUnit: extração por unidade, API por integração com SQL Server real) | 106 |
| Frontend (Karma e Jasmine: formulário e serviço HTTP) | 47 |

Extração das amostras de `samples/`, conferida contra o `esperado.json`:

| Resultado | Amostras |
|---|---|
| Nome, e-mail e telefone certos, em currículo legível | 4 de 4 (no sem telefone, o campo voltou vazio, como devia) |
| Nenhum campo e o aviso certo, em PDF ilegível (digitalizado, senha, corrompido) | 3 de 3 |
| Arquivo que não é PDF recusado com 400 | 1 de 1 |

Os acertos não medem a qualidade da extração em currículos reais: as amostras
foram escritas junto com as heurísticas, então servem para pegar regressão. Os
erros conhecidos estão nas limitações abaixo.

Tempo de resposta de `POST /api/curriculos/extracao`, medido com `curl` direto
na API em execução local (`dotnet run`), mediana de 20 chamadas depois de uma de
aquecimento:

| Amostra | Mediana | Máximo |
|---|---|---|
| `curriculo-completo.pdf` | 10,2 ms | 29,0 ms |
| `curriculo-duas-colunas.pdf` | 7,3 ms | 11,1 ms |
| `curriculo-sem-telefone.pdf` | 6,6 ms | 11,6 ms |
| `curriculo-nome-com-rotulo.pdf` | 6,3 ms | 8,1 ms |
| `curriculo-protegido-por-senha.pdf` | 5,0 ms | 23,8 ms |
| `curriculo-digitalizado.pdf` | 2,8 ms | 5,3 ms |
| `curriculo-corrompido.pdf` | 2,5 ms | 4,3 ms |

Além dos testes, cada fatia foi conferida à mão com a aplicação rodando, e ao
fim da fatia 8 um roteiro no Chrome headless percorreu os quatro fluxos nos dois
caminhos de execução.

## Erros da IA que precisei corrigir

Os detalhes estão em cada fatia; aqui, juntos:

- Registrou o provedor de animações sem instalar o pacote, quebrando o build
  por onze commits (fatia 1).
- Escreveu testes da tela de detalhes que a especificação excluía (fatia 3).
- Afirmou num comentário que o PdfPig embaralhava colunas, sem ter testado; o
  problema real era a falta de quebras de linha (fatia 5).
- Implementou um aviso único onde a especificação pedia uma lista, e as duas
  revisões automáticas não perceberam (fatia 6).
- Pôs uma linha de coautoria no commit `e72021c`, contra a regra que eu tinha
  dado de não fazer isso (entre as fatias 2 e 3).
- Afirmou neste registro que nenhum commit tinha build quebrado (fatia 8).

## Dificuldades

- **Ambiente dos testes de frontend:** o Chrome do puppeteer mudou de API na
  versão 25 e não baixava sem `unzip`; levou mais tempo que o próprio teste
  (fatia 1).
- **Texto do PDF:** o texto da página no PdfPig vem sem quebras de linha, e a
  heurística de nome trabalha linha a linha; foi preciso trocar pelo extrator
  que preserva as linhas (fatia 5).
- **Detalhes do ASP.NET que não aparecem no código:** upload acima de 64 KB vai
  para arquivo temporário, e o `dotnet ef` grava o `schema.sql` com BOM. Os dois
  só apareceram testando fora do caminho feliz (fatias 2 e 5).
- **Revisão automática não substitui ler a spec:** o aviso único da fatia 6
  passou pelas duas revisões e só apareceu no teste à mão.

## Tempo dedicado

Cerca de 7 horas e 10 minutos no total, contando o questionamento inicial, a
especificação e os tickets.

## Limitações conhecidas

| Limitação | Efeito para quem usa |
|---|---|
| PDF digitalizado (página como imagem), sem OCR | Nenhum campo é preenchido; aparece um aviso e os dados são digitados à mão. |
| Nome pela primeira linha de palavras capitalizadas, quando não há rótulo `Nome:` | Um cabeçalho como "Dados Pessoais" antes do nome vira o nome. Nome escrito todo em minúsculas não é reconhecido: vale a próxima linha que pareça nome, como "Experiência Profissional", ou o campo fica vazio. |
| Diagramação em colunas | O texto é lido na ordem em que foi gravado no arquivo. Uma coluna lateral com "Inglês Avançado" antes do nome faz dele o nome. |
| Telefone só nos formatos brasileiros, com DDD | Número de outro país fica vazio ou, como `+44 20 7946 0958`, vira um número brasileiro errado. Número sem DDD fica vazio. |
| Só nome, e-mail e telefone são extraídos, como pede o enunciado | Área ou cargo de interesse e resumo profissional são sempre digitados, mesmo que o currículo tenha seções de objetivo e resumo. |
| Mais de um e-mail ou telefone no currículo | Vale o primeiro que aparece, que pode ser o de uma referência. |
| Heurísticas em vez de um modelo de linguagem (LLM) | Previsível, testável, sem custo por currículo e sem enviar dados pessoais a um serviço externo. Em troca, erra em layouts fora do padrão que um LLM entenderia, e sobra mais campo para corrigir à mão. |

Em todos os casos o resultado é um palpite: os campos ficam editáveis e o
cadastro só é salvo depois que a pessoa do recrutamento confere.

## Melhorias com mais tempo

- **OCR para PDF digitalizado**, com Tesseract, que hoje só gera um aviso.
- **Ler a posição das palavras no PDF**, que o PdfPig fornece, para separar
  colunas e achar o nome pelo tamanho da fonte em vez da primeira linha
  capitalizada.
- **Ler área de interesse e resumo** pelas seções "Objetivo" e "Resumo".
- **Telefone formatado na exibição**, como `(11) 91234-5678`; hoje aparece só
  com dígitos.
- **Busca e paginação na listagem**, quando o volume justificar.
- **Testes end-to-end** com Playwright, transformando em teste o roteiro de
  Chrome headless usado na conferência manual.
