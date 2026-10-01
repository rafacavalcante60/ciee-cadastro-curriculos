# Registro do desenvolvimento

Como o trabalho foi organizado, que decisões tomei e como usei IA. Atualizado
ao fim de cada fatia de trabalho, não reconstruído no final.

## Resumo

- **Ferramenta:** Claude Code (Anthropic), modelo Claude Opus 5, como par de
  programação em todo o desafio.
- **Fluxo:** primeiro o modelo me questionou sobre o desenho (27 perguntas em
  quatro rodadas), depois escrevi a especificação e o recorte em sete tickets, e
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
3. **Sete tickets**, cada um uma fatia vertical verificável, com dependências
   declaradas: [#2 a #9](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues).

**Alternativa considerada:** o [spec-kit](https://github.com/github/spec-kit)
do GitHub, que gera especificação, plano e lista de tarefas como arquivos
versionados para cada funcionalidade. Esse formato compensa em projetos maiores
ou com várias pessoas. Aqui o escopo cabe em sete tickets, e issues no GitHub
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

## Limitações conhecidas

<!-- Consolidado na fatia de documentação, após a importação de PDF estar pronta. -->

## Melhorias com mais tempo

<!-- Consolidado na fatia de documentação. -->
