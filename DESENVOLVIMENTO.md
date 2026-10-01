# Registro do desenvolvimento

Relato de como o trabalho foi organizado, das decisões tomadas e da participação
de ferramentas de inteligência artificial. Escrito ao longo do desenvolvimento,
ao fim de cada fatia de trabalho, e não reconstruído no final.

## Ferramentas de IA utilizadas

- **Claude Code** (Anthropic), modelo **Claude Opus 5**, usado como par de
  programação ao longo de todo o desafio: discussão de desenho, redação da
  especificação e das issues, implementação e revisão.

O uso não foi "pedir o projeto pronto". O fluxo adotado foi: primeiro uma sessão
longa de questionamento em que o modelo me interrogou sobre as decisões em vez
de assumi-las; depois a especificação e o recorte em tickets; só então a
implementação, ticket a ticket.

## Organização do trabalho

Antes de escrever código, o trabalho passou por três etapas:

1. **Questionamento do desenho.** Vinte e sete perguntas em quatro rodadas, cada
   uma com uma recomendação e os motivos — escopo, infraestrutura, modelo de
   dados, contrato da API, estratégia de testes, documentação e critério de
   parada. Decidi contra a recomendação em dois pontos (ver abaixo).
2. **Especificação** publicada como issue, com problema, solução, histórias de
   usuário, decisões de implementação, decisões de teste e o que ficou fora de
   escopo.
3. **Recorte em sete tickets**, cada um uma fatia vertical verificável, com as
   dependências declaradas entre eles.

Os commits são pequenos, em português, seguindo Conventional Commits, e nenhum
entra com a compilação quebrada ou teste vermelho.

## Decisões em que discordei da recomendação do modelo

- **E-mail único.** A recomendação foi não ter restrição de unicidade, por não
  ter sido pedida. Discordei: cadastro duplicado de candidato é um problema real
  de recrutamento, e a mensagem de erro específica é mais útil que a ausência de
  verificação. Entrou com validação na aplicação mais índice único no banco.
- **Validação de formato de telefone.** A recomendação foi aceitar texto livre,
  para que um telefone em formato estranho vindo de um PDF não bloqueasse o
  cadastro. Preferi um controle simples. Ficou em dez ou onze dígitos após
  normalização, mantendo o campo opcional.

## Fatia 1 — Esqueleto do projeto e conexão com o banco

Monorepo com `frontend/` e `backend/`, SQL Server 2022 em container por compose,
infraestrutura de testes de pé nos dois lados e um endpoint de verificação de
saúde. Nenhum comportamento de produto ainda: o objetivo é que um clone novo
suba e que as fatias seguintes sejam verificáveis.

**Em que a IA ajudou.** Geração do esqueleto dos dois projetos e da
infraestrutura de teste de integração, redação do compose e do README. Exemplo
de pedido: implementar a fatia descrita no ticket de esqueleto, com fixture de
Testcontainers que aceite uma connection string por variável de ambiente.

**O que precisou de correção ou adaptação:**

- **O endpoint de saúde foi redesenhado.** A primeira versão respondia apenas
  "banco acessível ou não", via `CanConnectAsync`. Ao verificar contra o
  container, ficou claro que essa resposta confunde duas situações que pedem
  providências opostas: o container desligado e o banco ainda não criado. A
  versão final relata os dois separadamente, e devolve 503 só quando o servidor
  está inalcançável — quando o servidor responde mas o banco não existe, o
  próprio corpo da resposta diz para rodar a migration.
- **`puppeteer.executablePath()` passou a ser assíncrono** na versão 25, e a
  configuração do Karma tem de ser síncrona. O caminho do Chrome passou a ser
  resolvido por `computeExecutablePath` do `@puppeteer/browsers`, usando o
  identificador de build que o puppeteer expõe — solução síncrona e que funciona
  nas três plataformas.
- **O download do Chrome falhava sem mensagem clara.** A instalação do puppeteer
  deixava os diretórios de cache vazios. A causa, encontrada só ao rodar a
  instalação manualmente, era a ausência do utilitário `unzip` no sistema.
  Resolvido adicionando a dependência opcional `yauzl` ao projeto, em vez de
  exigir um pacote do sistema — assim a integração contínua também não depende
  disso.
- **Bibliotecas de sistema do Chrome.** Mesmo instalado, o Chrome não inicia sem
  `libnss3` e `libasound2t64`, ausentes nesta instalação enxuta do Ubuntu. É o
  único passo do projeto que exige instalação no sistema, e está documentado no
  README.
- **`appsettings.Development.json` não recebeu senha funcional.** Seria o
  caminho mais confortável para quem avalia, mas versionaria credencial. Ficou
  placeholder no `appsettings.json` e as credenciais reais vêm de variável de
  ambiente ou de `dotnet user-secrets`.

**Como verifiquei.** Três testes de integração contra SQL Server real, subido
por Testcontainers: servidor alcançável, banco da aplicação ausente relatado
como tal, e 503 quando o servidor não responde. Além dos testes, verificação
manual com a aplicação rodando contra o container do compose, conferindo a
resposta de `/api/saude` e o documento do Swagger.

**Revisão da fatia.** Submeti a fatia a uma revisão em dois eixos independentes,
um conferindo aderência aos padrões do projeto e outro conferindo fidelidade ao
ticket. Achados que viraram correção:

- **Senha de desenvolvimento no README.** O passo de criação do schema trazia a
  senha real do `.env.example`, oito linhas depois de o próprio README afirmar
  que nenhuma credencial é versionada. Trocada por `SUA_SENHA`. A senha
  permanece apenas no `.env.example`, que é um arquivo de exemplo por
  definição — essa parte eu mantive de propósito, porque o ticket pede um
  exemplo que satisfaça a política de complexidade do SQL Server, e um
  placeholder ali faria o container falhar num clone novo.
- **Falha 500 em connection string malformada.** O método que verifica o
  servidor capturava apenas `SqlException`. Sondando os casos reais, descobri
  que configuração ausente, vazia ou com o placeholder respondem 503
  corretamente, mas uma string **malformada** estoura `ArgumentException` na
  montagem da conexão, antes de qualquer tentativa de rede, e vazava como 500 —
  justamente o erro de configuração que o endpoint existe para diagnosticar.
  Corrigido, com teste de regressão parametrizado nos três casos.
- **Angular Material sem o provedor de animações.** O `ng add` foi executado com
  animações habilitadas, mas não registrou `provideAnimationsAsync()` na
  configuração da aplicação. Teria falhado no primeiro componente de Material da
  fatia seguinte. Adicionado junto com `provideHttpClient()`.
- **README citava `docs/adr/`, que ainda não existe.** Linha removida; os ADRs
  entram na fatia de documentação.
- **Promessa a mais no README.** O passo de criação do schema não tem efeito
  enquanto não houver a primeira migration, que pertence à fatia de cadastro.
  O texto agora diz isso, em vez de deixar quem avalia achando que algo quebrou.
- **Comentários em inglês herdados do scaffold** do Karma, e o jargão "seam" sem
  tradução. Ajustados: o projeto usa português de forma consistente.

Um achado eu rejeitei: a sugestão de transformar os campos de situação da
resposta de saúde em tipo próprio, em vez de texto. São quatro valores num
endpoint de diagnóstico; um enum com conversor de serialização seria mais
maquinaria do que o problema pede.

**Tempo dedicado.** <!-- a preencher -->

## Limitações conhecidas

<!-- Consolidado na fatia de documentação, após a importação de PDF estar pronta. -->

## Melhorias com mais tempo

<!-- Consolidado na fatia de documentação. -->
