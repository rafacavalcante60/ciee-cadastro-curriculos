using CieeCurriculos.Api.Dados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var construtor = WebApplication.CreateBuilder(args);

// Chaves dos erros de validação com o nome do campo no JSON (nomeCompleto), não o
// da propriedade C# (NomeCompleto): o frontend associa cada erro ao seu campo.
construtor.Services.AddControllers(opcoes =>
    opcoes.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()));
construtor.Services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = contexto =>
{
    if (contexto.ProblemDetails is ValidationProblemDetails)
    {
        contexto.ProblemDetails.Title = "Um ou mais campos estão inválidos.";
    }
});
construtor.Services.AddSingleton(TimeProvider.System);
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

construtor.Services.AddDbContext<CurriculosDbContext>(opcoes =>
    opcoes.UseSqlServer(construtor.Configuration.GetConnectionString("CurriculosDb")));

var aplicacao = construtor.Build();

if (aplicacao.Environment.IsDevelopment())
{
    aplicacao.UseSwagger();
    aplicacao.UseSwaggerUI();
}

// Migrations não são aplicadas aqui: criar o schema é um passo explícito do README.

aplicacao.MapControllers();

aplicacao.Run();

/// <summary>
/// Declarada parcial e pública para que os testes de integração possam
/// instanciar a aplicação com WebApplicationFactory.
/// </summary>
public partial class Program;
