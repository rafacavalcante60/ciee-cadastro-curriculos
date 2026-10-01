# Cadastro de currículos

Aplicação para a equipe de recrutamento cadastrar e consultar candidatos. O
cadastro acontece por dois caminhos que compartilham o mesmo formulário e as
mesmas regras de validação: preenchimento manual, ou envio de um currículo em
PDF do qual a aplicação tenta extrair nome, e-mail e telefone.

A [especificação](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/1)
e os [tickets](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues?q=is%3Aissue)
estão nas issues do repositório. Como o trabalho foi feito, com as decisões, o
uso de IA e os números medidos, está no [registro do desenvolvimento](DESENVOLVIMENTO.md).

## Tecnologias e versões

| Camada | Tecnologia | Versão |
| --- | --- | --- |
| Frontend | Angular (componentes standalone, Reactive Forms, Angular Material) | 19.2 |
| | Node.js / npm | 22 / 10 |
| | Nginx, servindo o frontend no caminho Docker | 1.27 |
| Backend | ASP.NET Core com Controllers | .NET 8.0 |
| | Entity Framework Core (SQL Server) | 8.0 |
| | PdfPig, leitura do texto do PDF | 0.1.16 |
| Banco | SQL Server (em container) | 2022 |
| Testes | xUnit, Testcontainers, `WebApplicationFactory` | 2.4 / 4.0 / 8.0 |
| | Karma, Jasmine, Chrome via puppeteer | 6.4 / 5.6 / 25 |
| Amostras | QuestPDF, gerador dos currículos fictícios | 2026.9 |
| Infraestrutura | Docker Compose, GitHub Actions | — |

## Como rodar

### Com Docker (recomendado)

Só é preciso Docker com Docker Compose; nada de .NET, Node ou `dotnet-ef`.

```bash
git clone https://github.com/rafacavalcante60/ciee-cadastro-curriculos.git && cd ciee-cadastro-curriculos
cp .env.example .env
docker compose up --build
```

A aplicação abre em `http://localhost:8080` (a porta muda com `FRONTEND_PORTA`
no `.env`). A subida segue uma ordem: o SQL Server fica saudável, um serviço de
migração cria o banco e aplica o `backend/schema.sql` e termina, a API sobe, e
por fim o Nginx, que serve o frontend e repassa `/api/` para a API. Subir de
novo sobre os mesmos dados não falha, porque o script é idempotente. Para
começar do zero: `docker compose down -v`.

A primeira subida baixa cerca de 1,3 GB, na maior parte a imagem do SQL Server,
e leva alguns minutos; as seguintes usam o cache e sobem em segundos. Em Mac com
chip Apple, a imagem do SQL Server roda por emulação: deixe ligada a opção "Use
Rosetta for x86_64/amd64 emulation" do Docker Desktop.

### Com os SDKs (desenvolvimento e testes)

Pré-requisitos:

- .NET SDK 8
- Node.js 22
- Docker com Docker Compose
- Ferramenta de migrations: `dotnet tool install --global dotnet-ef --version 8.*`

```bash
# 1. Configure as credenciais locais
#    O .env fica fora do repositório; ajuste a senha se quiser.
cp .env.example .env

# 2. Suba só o SQL Server
docker compose up -d banco

# 3. Crie a estrutura do banco
cd backend
export ConnectionStrings__CurriculosDb="Server=localhost,1433;Database=CieeCurriculos;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True"
dotnet ef database update --project CieeCurriculos.Api

# 4. Rode a API e, em outro terminal, o frontend
dotnet run --project CieeCurriculos.Api     # http://localhost:5080
cd ../frontend && npm install && npm start  # http://localhost:4200
```

Sem a ferramenta `dotnet-ef`, o passo 3 pode ser feito pelo mesmo serviço de
migração do caminho Docker, que aplica o `backend/schema.sql` com o `sqlcmd` da
imagem do SQL Server:

```bash
docker compose run --rm migracao
```

A API fica em `http://localhost:5080`, com Swagger em `/swagger`. O frontend
chama caminhos relativos (`/api/...`) e o servidor de desenvolvimento do Angular
os repassa para a API, conforme `frontend/proxy.conf.json` — por isso não há
configuração de CORS.

Para conferir se a configuração está correta, antes mesmo de abrir a interface:

```bash
curl http://localhost:5080/api/saude
```

A resposta distingue três situações: a API no ar, o servidor de banco
alcançável, e o banco da aplicação já criado. Se o banco aparecer como
`ausente`, falta o passo 3.

### Configurando a connection string

O `appsettings.json` versionado traz um **placeholder**, não uma senha. Escolha
uma das formas de fornecer a connection string de verdade:

```bash
# variável de ambiente
export ConnectionStrings__CurriculosDb="Server=localhost,1433;Database=CieeCurriculos;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True"

# ou segredos de desenvolvimento, que ficam fora do repositório
cd backend/CieeCurriculos.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:CurriculosDb" "Server=localhost,1433;..."
```

A senha do SQL Server precisa satisfazer a política de complexidade do produto
(mínimo de 8 caracteres, com maiúscula, minúscula, dígito e símbolo). Uma senha
fraca faz o container subir e encerrar em seguida.

## Testes

```bash
# Backend: extração por unidade, API por integração contra SQL Server real
cd backend && dotnet test

# Frontend: Karma e Jasmine em Chrome headless
cd frontend && npm test
```

Os testes de integração do backend sobem um SQL Server próprio via
Testcontainers, isolado do banco de desenvolvimento. Para apontá-los para um
banco já existente, defina `TEST_SQL_CONNECTION` com a connection string.

O Chrome usado nos testes de frontend é instalado pelo puppeteer como
dependência de desenvolvimento, então não é preciso ter navegador no sistema. Em
distribuições enxutas, o Chrome ainda depende de bibliotecas do sistema; no
Ubuntu:

```bash
sudo apt-get install -y libnss3 libasound2t64
```

Os dois conjuntos rodam a cada push e pull request no
[GitHub Actions](.github/workflows/ci.yml): build e testes do backend, testes e
build do frontend.

## Amostras de currículo

A pasta [`samples/`](samples/README.md) traz currículos fictícios em PDF para
testar a importação à mão, pelo botão "Preencher a partir de um PDF" ou pelo
Swagger. Um teste de integração envia cada um à API e compara a resposta com o
`samples/esperado.json`.

| Arquivo | O que exercita |
| --- | --- |
| `curriculo-completo.pdf` | Caminho feliz: nome, e-mail e telefone encontrados. |
| `curriculo-sem-telefone.pdf` | Telefone ausente fica vazio, com aviso; os períodos `2021 – 2023` não viram telefone. |
| `curriculo-nome-com-rotulo.pdf` | Rótulo `Nome completo:` depois de um título que também parece nome, CPF e CEP que não podem virar telefone, celular com `+55`. |
| `curriculo-duas-colunas.pdf` | Coluna lateral de contato lida antes da coluna onde está o nome. |
| `curriculo-digitalizado.pdf` | Página como imagem, sem texto: nenhum campo preenchido, com aviso. |
| `curriculo-protegido-por-senha.pdf` | PDF criptografado: nenhum campo preenchido, com aviso de senha. |
| `curriculo-corrompido.pdf` | PDF cortado ao meio: nenhum campo preenchido, com aviso. |
| `nao-e-pdf.pdf` | Texto com extensão `.pdf`: recusado com 400 pela assinatura do arquivo. |

## Estrutura

```
backend/     API em ASP.NET Core, projeto de testes e schema.sql
frontend/    Aplicação Angular e configuração do Nginx
samples/     Currículos fictícios, resultado esperado e o gerador
.github/     Workflow de integração contínua
```

## Decisões técnicas

- **PdfPig para ler o PDF**, por licença: é Apache 2.0. O iText 7, mais
  conhecido, é AGPL, o que obrigaria a abrir o código de um produto comercial
  que o usasse. PDFsharp foi descartado por extrair texto mal, e Docnet.Core por
  depender de binário nativo.
- **Extração por heurística, sem modelo de linguagem (LLM).** Um LLM acertaria
  mais o nome, mas exigiria uma chave de API que quem avalia não tem, e traria
  custo por currículo, latência, falha de rede e dados pessoais enviados a um
  serviço externo. A heurística é previsível e testável. Os casos em que ela
  erra estão nas [limitações conhecidas](DESENVOLVIMENTO.md#limitações-conhecidas),
  e os campos sempre ficam editáveis antes de salvar.
- **Um projeto de API e um de testes, organizados por pastas** (`Candidatos/`,
  `Curriculos/`, `Dados/`), sem Clean Architecture, MediatR ou CQRS. Para uma
  entidade só, as camadas extras não trariam benefício que eu conseguisse
  defender.
- **Schema como passo explícito**, por migration ou pelo serviço `migracao` do
  compose. A API não altera o banco ao subir.
- **Erros no formato ProblemDetails**, nativo do ASP.NET Core, com mensagens em
  português e o nome do campo, para o formulário mostrar cada erro junto ao
  campo certo.

O motivo de cada decisão, fatia a fatia, está no
[registro do desenvolvimento](DESENVOLVIMENTO.md).

## Fora de escopo

Não implementado de propósito, para manter a solução no tamanho do que foi
pedido:

- **Autenticação e autorização**: não foram pedidas.
- **Edição e exclusão de candidatos**: o enunciado pede cadastrar e consultar.
- **Paginação, busca e filtros na listagem**: não foram pedidos; a listagem
  traz todos os candidatos, do mais recente para o mais antigo.
- **Guardar o arquivo PDF**: só os campos são salvos. Guardar o binário
  traria armazenamento, limpeza e o risco de reter dados pessoais, sem ganho
  para o que foi pedido.
- **Ler outros campos do PDF**, como área de interesse e resumo: o enunciado
  pede nome, e-mail e telefone.
- **OCR para PDF digitalizado**: o aviso orienta a digitar os dados.
- **Testes end-to-end**: as regras são cobertas por testes de integração da
  API e por testes do formulário e do serviço HTTP no frontend; os quatro
  fluxos foram conferidos à mão no navegador.
- **Deduplicação por nome ou telefone**: só o e-mail é único.
- **Validação de DDD existente e do nono dígito** no telefone: aceita dez ou
  onze dígitos.
- **Outros idiomas**: a aplicação é só em português.
- **Implantação em nuvem**: a aplicação roda localmente com um comando.
