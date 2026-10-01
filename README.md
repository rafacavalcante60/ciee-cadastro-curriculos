# Cadastro de currículos

Aplicação para a equipe de recrutamento cadastrar e consultar candidatos. O
cadastro acontece por dois caminhos que compartilham o mesmo formulário e as
mesmas regras de validação: preenchimento manual, ou envio de um currículo em
PDF do qual a aplicação tenta extrair nome, e-mail e telefone.

> **Estado atual:** cadastro manual e listagem de candidatos funcionando. A tela
> de detalhes, as validações completas e a importação de PDF estão em
> desenvolvimento. A
> [especificação](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues/1)
> e os [tickets](https://github.com/rafacavalcante60/ciee-cadastro-curriculos/issues)
> estão nas issues do repositório.

## Tecnologias e versões

| Camada | Tecnologia | Versão |
| --- | --- | --- |
| Frontend | Angular (componentes standalone, Reactive Forms, Angular Material) | 19.2 |
| | Node.js / npm | 22.23 / 10.9 |
| Backend | ASP.NET Core com Controllers | .NET 8.0 |
| | Entity Framework Core (SQL Server) | 8.0 |
| Banco | SQL Server (em container) | 2022 |
| Testes | xUnit, Testcontainers, `WebApplicationFactory` | — |
| | Karma, Jasmine, Chrome via puppeteer | — |

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
# Backend: integração contra SQL Server real
cd backend && dotnet test

# Frontend: Karma e Jasmine em Chrome headless
cd frontend && npm test
```

Os testes de integração do backend sobem um SQL Server próprio via
Testcontainers, isolado do banco de desenvolvimento. Para apontá-los para um
banco já existente, defina `TEST_SQL_CONNECTION` com a connection string — útil
na integração contínua.

O Chrome usado nos testes de frontend é instalado pelo puppeteer como
dependência de desenvolvimento, então não é preciso ter navegador no sistema. Em
distribuições enxutas, o Chrome ainda depende de bibliotecas do sistema; no
Ubuntu:

```bash
sudo apt-get install -y libnss3 libasound2t64
```

## Estrutura

```
backend/    API em ASP.NET Core, projeto de testes e schema.sql
frontend/   Aplicação Angular e configuração do Nginx
GLOSSARY.md Vocabulário do domínio
```

## Fora de escopo

Deliberadamente não implementado, para manter a solução simples e dentro do que
foi pedido: autenticação, edição e exclusão de candidatos, paginação, busca e
filtros na listagem, armazenamento do arquivo PDF, implantação em nuvem, testes
end-to-end, e uso de modelo de linguagem em tempo de execução para a extração.
