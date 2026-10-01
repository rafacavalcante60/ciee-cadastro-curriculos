using CieeCurriculos.Api.Dados;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var construtor = WebApplication.CreateBuilder(args);

construtor.Services.AddControllers();
construtor.Services.AddEndpointsApiExplorer();
construtor.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cadastro de currículos",
        Version = "v1",
        Description = "API de cadastro e consulta de candidatos, com importação de currículo em PDF."
    });
});

// A connection string vem de appsettings (placeholder), sobrescrita por
// variável de ambiente (ConnectionStrings__CurriculosDb) ou por dotnet user-secrets.
construtor.Services.AddDbContext<CurriculosDbContext>(opcoes =>
    opcoes.UseSqlServer(construtor.Configuration.GetConnectionString("CurriculosDb")));

var aplicacao = construtor.Build();

if (aplicacao.Environment.IsDevelopment())
{
    aplicacao.UseSwagger();
    aplicacao.UseSwaggerUI();
}

// Nenhuma migration é aplicada automaticamente aqui, por decisão de projeto:
// criar o schema é um passo explícito, documentado no README.

aplicacao.MapControllers();

aplicacao.Run();

/// <summary>
/// Declarada parcial e pública para que os testes de integração possam
/// instanciar a aplicação com WebApplicationFactory.
/// </summary>
public partial class Program;
