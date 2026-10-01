using Microsoft.EntityFrameworkCore;

namespace CieeCurriculos.Api.Dados;

/// <summary>
/// Contexto de acesso aos dados do cadastro de currículos.
/// Ainda não possui entidades: o <c>Candidato</c> entra na fatia de cadastro manual.
/// </summary>
public class CurriculosDbContext : DbContext
{
    public CurriculosDbContext(DbContextOptions<CurriculosDbContext> opcoes) : base(opcoes)
    {
    }
}
