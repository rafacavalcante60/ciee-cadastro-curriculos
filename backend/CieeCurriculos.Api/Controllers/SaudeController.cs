using CieeCurriculos.Api.Dados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CieeCurriculos.Api.Controllers;

[ApiController]
[Route("api/saude")]
public class SaudeController : ControllerBase
{
    private readonly CurriculosDbContext _contexto;

    public SaudeController(CurriculosDbContext contexto)
    {
        _contexto = contexto;
    }

    // Servidor inacessível e banco ainda não criado pedem providências diferentes
    // (subir o container ou criar o schema), por isso são relatados separados.
    [HttpGet]
    [ProducesResponseType(typeof(RespostaSaude), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Obter(CancellationToken cancelamento)
    {
        if (!await ServidorAcessivelAsync(cancelamento))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Servidor de banco de dados inacessível",
                Detail = "A API está no ar, mas não alcançou o SQL Server. Confira se o container do "
                       + "banco está em execução e se a connection string está configurada."
            });
        }

        var bancoDaAplicacaoExiste = await _contexto.Database.CanConnectAsync(cancelamento);

        return Ok(new RespostaSaude(
            Api: "ok",
            Servidor: "ok",
            BancoDaAplicacao: bancoDaAplicacaoExiste ? "ok" : "ausente",
            Detalhe: bancoDaAplicacaoExiste
                ? null
                : "O servidor responde, mas o banco da aplicação não existe. Crie a estrutura com 'dotnet ef database update'."));
    }

    // Conecta ao master para saber se o servidor responde mesmo sem o banco da aplicação.
    private async Task<bool> ServidorAcessivelAsync(CancellationToken cancelamento)
    {
        try
        {
            var conexaoAdministrativa = new SqlConnectionStringBuilder(_contexto.Database.GetConnectionString())
            {
                InitialCatalog = "master"
            };

            await using var conexao = new SqlConnection(conexaoAdministrativa.ConnectionString);
            await conexao.OpenAsync(cancelamento);
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            // Connection string malformada: também é erro de configuração, então 503.
            return false;
        }
    }
}

public record RespostaSaude(string Api, string Servidor, string BancoDaAplicacao, string? Detalhe);
