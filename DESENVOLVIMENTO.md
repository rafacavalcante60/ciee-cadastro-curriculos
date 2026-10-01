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

## Fatia 4: validações e mensagens de erro

Ticket: [#5](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/5).

Regras de nome, e-mail, telefone e tamanho dos campos na API e no formulário,
com mensagens em português junto a cada campo. E-mail repetido devolve 409 e
aparece no campo de e-mail. O cadastro bem-sucedido mostra um aviso, e a API
fora do ar gera uma mensagem própria. **Tempo:** cerca de 20 minutos,
incluindo revisão.

**Onde a IA ajudou:** testes no seam HTTP, no formulário e no
`CandidatoService` escritos antes do código, um ciclo por regra; conferência
do formulário no navegador; revisão da fatia.

**Decisões desta fatia:**

- **Atributos de validação (DataAnnotations).** São nativos, o
  `[ApiController]` já os converte em `ValidationProblemDetails`, e cada regra
  fica visível na própria propriedade de `NovoCandidato`. FluentValidation
  seria mais uma biblioteca para explicar, sem ganho num formulário de cinco
  campos.
- **Erros com o nome do campo no JSON.** A API devolvia os erros com a chave
  `NomeCompleto`, enquanto o frontend envia `nomeCompleto`. O
  `SystemTextJsonValidationMetadataProvider` passa a usar o nome do JSON, e o
  formulário associa cada erro ao seu campo sem tradução de nomes. O provedor
  não funciona com parâmetros de construtor de record, por isso `NovoCandidato`
  passou a ter propriedades `init`.
- **Mesma regra nos dois lados, ao pé da letra.** A expressão do e-mail e a
  normalização do telefone são as mesmas em C# e em TypeScript. Assim não há
  um e-mail que o formulário aceita e a API recusa. Campo em branco gera só
  "Informe…", e não também a mensagem de formato, que é o comportamento dos
  validadores do Angular.
- **Telefone gravado só com dígitos.** A normalização remove espaços,
  parênteses, hífen e ponto. Qualquer outro caractere, inclusive o `+` do
  `+55`, sobra e reprova. A extração do PDF (#6) remove o `+55` antes de
  preencher o formulário.
- **E-mail único em dois níveis.** A checagem antes do insert cobre o caso
  comum. A violação do índice único (erros 2601 e 2627 do SQL Server) é
  capturada e também vira 409, para a corrida entre dois cadastros simultâneos.
- **409 como ProblemDetails comum, não como erro de validação.** O
  `CandidatoService` traduz o 409 em erro no campo de e-mail. O contrato da API
  continua padrão, e a tradução fica num lugar só, coberto por teste.
- **"Servidor indisponível" para falha de rede e 5xx.** Com a API fora do ar, o
  proxy do `ng serve` responde 500 com corpo vazio (conferido na prática), e o
  Nginx responde 502. Pelo status não dá para separar API caída de erro
  interno, então os dois mostram a mesma mensagem, escrita para não afirmar
  que a API está fora.
- **Tamanho máximo validado.** Texto acima do tamanho da coluna devolve 400 em
  vez de 500, resolvendo a pendência da fatia 2.

**O que corrigi ou adaptei:**

- O título do `ValidationProblemDetails` continuava em inglês ("One or more
  validation errors occurred."). Percebi ao chamar a API à mão; os testes só
  conferiam as mensagens dos campos. O teste passou a conferir o título, e a
  tradução foi feita por `AddProblemDetails`.
- O primeiro teste dos dois cenários de sucesso usava `whenStable`, que nunca
  terminava: o aviso de sucesso fica aberto cinco segundos e esse timer
  impedia a estabilização. Os dois passaram a usar `fakeAsync`.

**Achados da revisão:**

- Nome com espaços nas pontas (`" M "`) passava pelo mínimo de 2 caracteres,
  dos dois lados. O mínimo passou a ignorar os espaços, e o nome é gravado
  aparado, como o e-mail.
- A mensagem de reserva do 409 no frontend era diferente da mensagem da API.
  Igualada.
- A mensagem de servidor indisponível mandava verificar se a API estava em
  execução, o que engana quando o 500 vem de um erro da própria API.
  Reescrita.
- `TelefoneAttribute` virou `FormatoDeTelefoneAttribute`, no padrão dos outros
  atributos, e o tipo do estado de falha do formulário passou a ser derivado de
  `FalhaAoCadastrar`.
- **Rejeitado:** traduzir as mensagens de erro de desserialização (JSON
  malformado, corpo vazio). O formulário nunca envia esses casos, e
  traduzi-las exigiria substituir o tratamento do framework.
- **Rejeitado:** um mapa de mensagens por campo no formulário, no lugar da
  cascata de `if`. São cinco campos fixos, e as mensagens ficam lidas num
  lugar só.
- **Rejeitado:** extrair o corpo de `IsValid` repetido em dois atributos e a
  mensagem "Use no máximo…" repetida em quatro. É repetição curta e visível, e
  um nível de indireção a mais custaria mais para ler.
- **Rejeitado:** aplicar o tamanho máximo do e-mail depois de aparar os
  espaços. Só afeta um e-mail de quase 256 caracteres com espaços nas pontas.

**Como verifiquei:**

- Testes de integração contra SQL Server real, um por regra: nome, e-mail e
  telefone ausentes ou inválidos, telefone aceito com e sem máscara e gravado
  só com dígitos, opcionais vazios aceitos, texto acima do tamanho, e-mail
  repetido, inclusive com maiúsculas e espaços, e dez cadastros simultâneos com
  o mesmo e-mail resultando em um 201 e nove 409.
- Para confirmar que o teste de cadastros simultâneos passa mesmo pela captura
  do índice, troquei temporariamente a resposta da captura por 418. Nas três
  execuções, os nove cadastros recusados vieram por ela, e não pela checagem
  prévia. Restaurei o código.
- Testes do formulário para cada regra e para os erros vindos da API, e do
  `CandidatoService` para a tradução de 400, 409, falha de rede e 500.
- No navegador: formulário vazio e com dados inválidos mostrando a mensagem
  sob cada campo; e-mail repetido digitado como `  Maria.Silva@Exemplo.COM `
  mostrando o 409 no campo de e-mail, sem alerta genérico, e a mensagem
  sumindo ao editar o campo; cadastro aceito com aviso na listagem e telefone
  `(11) 98765-4321` gravado como `11987654321`; API desligada mostrando a
  mensagem de servidor indisponível.

## Fatia 5: importação de currículo em PDF

Ticket: [#6](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/6).

`POST /api/curriculos/extracao` recebe um PDF e devolve nome, e-mail e
telefone identificados, com `null` no que não encontrou. Na tela de cadastro,
um botão acima do formulário envia o arquivo e preenche os campos, que
continuam editáveis. O salvamento segue sendo o mesmo `POST` de JSON. Esta
fatia cobre o caminho feliz e a recusa de arquivo inválido; PDF sem texto,
corrompido ou protegido fica para a #7.

**Onde a IA ajudou:** heurísticas de extração em TDD, um ciclo por regra;
gerador das amostras com QuestPDF; testes no seam HTTP, no formulário e no
`CandidatoService`; conferência no navegador; revisão da fatia.

**Decisões desta fatia:**

- **Extração como função de texto para campos.** `ExtracaoDeCampos.Extrair`
  recebe uma `string` e devolve `CamposExtraidos`, sem saber de HTTP nem de
  PDF. É uma função estática, não uma interface C# registrada em DI: há uma
  única implementação, e uma interface seria uma camada sem uso. Os 41 casos
  de unidade rodam em milissegundos, sem gerar um PDF por caso.
- **Leitura pela ordem do conteúdo.** `page.Text` do PdfPig junta a página
  inteira sem quebra de linha (testei: o e-mail grudava na palavra anterior e
  nenhum nome era encontrado). `ContentOrderTextExtractor` devolve uma linha
  por linha do documento, que é como a heurística do nome trabalha.
- **CPF não vira telefone.** Números logo depois dos rótulos CPF, CNPJ, CEP e
  RG são apagados antes de procurar o telefone. O `+55` fica fora do grupo que
  vira o número, então o resultado tem os 10 ou 11 dígitos que a validação do
  cadastro aceita.
- **Nome:** rótulo `Nome:` ou `Nome completo:` primeiro; senão, a primeira
  linha só com palavras capitalizadas (duas ou mais, fora as partículas) que
  não tenha termos de título como "Currículo". Linhas de e-mail e telefone
  caem fora por terem caracteres que não são letras. Nome todo em maiúsculas
  ganha iniciais maiúsculas, com `da`, `de`, `do`, `das`, `dos` e `e` em
  minúsculas.
- **Tipo do arquivo pela assinatura `%PDF-`**, não pela extensão nem pelo
  content-type, que o cliente declara como quiser.
- **Binário só em memória.** Por padrão o ASP.NET grava em arquivo temporário
  todo upload acima de 64 KB. O endpoint eleva esse limiar com
  `[RequestFormLimits(MemoryBufferThreshold = ...)]`. O limite de 5 MB é
  conferido no controller, porque o `MultipartBodyLengthLimit` recusaria com o
  erro genérico de validação, sem dizer que o problema é o tamanho.
- **Limite de 5 MB também no formulário**, como as demais regras da API: o
  arquivo grande é recusado antes de subir.
- **Campo não identificado apaga o valor anterior.** Ao enviar um segundo
  currículo, um campo que ele não traz ficaria com o dado do primeiro
  candidato. Apagar é o que o ticket pede ("fica vazio") e evita misturar
  duas pessoas.
- **Amostras commitadas, geradas por código.** O gerador fica em
  `samples/gerador/`, fora da solução, para que `dotnet test` não baixe o
  QuestPDF. Os PDFs saem iguais byte a byte ao rodar de novo (conferido por
  hash), porque as datas dos metadados são fixas. O teste percorre os PDFs da
  pasta e falha se algum não estiver em `esperado.json`.

**O que corrigi ou adaptei:**

- O primeiro comentário do `LeitorDePdf` dizia que a leitura por posição
  juntava as duas colunas numa linha. Era suposição. Troquei temporariamente
  para `page.Text` para conferir, e o problema real era outro (falta de
  quebras de linha). Reescrevi o comentário com o que observei.
- Confirmei na prática que o arquivo não vai para o disco: com
  `ASPNETCORE_TEMP` apontando para uma pasta sem permissão de escrita, o
  upload falhou ao tentar criar `ASPNETCORE_*.tmp` sem o atributo e passou com
  ele. Não virou teste automático, porque essa pasta é lida uma vez por
  processo e o teste dependeria da ordem de execução.
- Um script editou o `.csproj` de testes e trocou as quebras de linha CRLF do
  arquivo por LF, inflando o diff. Refiz a edição preservando o original.

**Achados da revisão:**

- Nome com hífen ou apóstrofo em maiúsculas virava "Maria-josé" e "D'ávila".
  Agora hífen e apóstrofo também abrem inicial, com teste.
- O padrão do rótulo de documento aceitava até 10 caracteres quaisquer antes
  do número, e em "RG — Tel: 11 98765-4321" apagava o DDD do telefone. Agora
  só aceita separadores e `nº`, com teste.
- "Importar" não está no glossário: o método e a classe do formulário
  passaram a usar "extração".
- O teste que conferia se todo PDF tinha resultado declarado olhava arquivos,
  não comportamento. Foi incorporado ao teste das amostras, que agora percorre
  a pasta.
- A tradução de 5xx e falha de rede estava copiada no `CandidatoService`, sem
  o comentário que explica o 500 do proxy. Extraída para `falhaGenerica`.
- Comentários que repetiam o nome do teste ou do arquivo, removidos.
- **Rejeitado:** concentrar o limite de 5 MB num lugar só. Ele aparece na API
  e no formulário porque o projeto repete as regras da API no frontend de
  propósito, como nas validações da fatia 4.
- **Rejeitado:** interface C# para o serviço de extração (motivo acima).
- **Fica para a #7:** um cabeçalho como "DADOS PESSOAIS", sem rótulo e antes
  do nome, é sugerido como nome. Bate com a regra pedida, mas entra nas
  limitações documentadas.

**Como verifiquei:**

- 41 testes de unidade da extração: cada formato de telefone, `+55`, CPF,
  CNPJ, CEP e RG, nome por posição e por rótulo, maiúsculas, e as combinações
  de campo presente e ausente. Cada regra falhou antes de ser implementada.
- Testes no seam HTTP: PDF completo devolve os três campos; as quatro
  amostras batem com `esperado.json`; a extração não cria candidato e os
  campos devolvidos concluem o cadastro; arquivo com extensão `.pdf` que não é
  PDF, arquivos de 5 MB + 1 byte e de 12 MB, e requisição sem arquivo
  devolvem 400.
- Testes do formulário (campos preenchidos e editáveis, carregamento, campo
  não identificado vazio, erro da API, arquivo grande) e do `CandidatoService`.
- À mão, com `curl`: as cinco amostras, arquivos de 6 MB e de 40 MB, e o
  Swagger mostrando o campo de upload. O de 40 MB passa do teto do Kestrel
  (30 MB) e volta 400 com a mensagem genérica do framework, em inglês.
- No navegador: o botão mostra "Lendo currículo…" e fica desabilitado durante
  o envio; o formulário é preenchido; `nao-e-pdf.pdf` e um arquivo de 6 MB
  mostram o motivo da recusa; com o nome editado, o cadastro salvou e
  apareceu na listagem.

## Limitações conhecidas

<!-- Consolidado na fatia de documentação, após a importação de PDF estar pronta. -->

## Melhorias com mais tempo

<!-- Consolidado na fatia de documentação. -->
