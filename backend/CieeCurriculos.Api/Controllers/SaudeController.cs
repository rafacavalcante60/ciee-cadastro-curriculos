using CieeCurriculos.Api.Dados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CieeCurriculos.Api.Controllers;

/// <summary>
/// Verificação de que a API está no ar e consegue falar com o SQL Server.
/// Serve para confirmar a configuração de um clone novo antes de usar a aplicação.
/// </summary>
[ApiController]
[Route("api/saude")]
public class SaudeController : ControllerBase
{
    private readonly CurriculosDbContext _contexto;

    public SaudeController(CurriculosDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <summary>
    /// Informa a situação da API, do servidor de banco e do banco da aplicação.
    /// </summary>
    /// <remarks>
    /// Servidor inacessível e banco ainda não criado exigem providências opostas de
    /// quem está configurando o projeto — subir o container contra criar o schema —,
    /// por isso são relatados em campos separados em vez de uma única resposta de falha.
    /// </remarks>
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

    /// <summary>
    /// Verifica se o servidor responde, independentemente de o banco da aplicação
    /// já ter sido criado, conectando ao catálogo administrativo.
    /// </summary>
    private async Task<bool> ServidorAcessivelAsync(CancellationToken cancelamento)
    {
        var construtor = new SqlConnectionStringBuilder(_contexto.Database.GetConnectionString())
        {
            InitialCatalog = "master"
        };

        try
        {
            await using var conexao = new SqlConnection(construtor.ConnectionString);
            await conexao.OpenAsync(cancelamento);
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }
}

/// <summary>Resultado da verificação de saúde da aplicação.</summary>
/// <param name="Api">Situação da API.</param>
/// <param name="Servidor">Situação do servidor de banco de dados.</param>
/// <param name="BancoDaAplicacao">Situação do banco da aplicação: <c>ok</c> ou <c>ausente</c>.</param>
/// <param name="Detalhe">Orientação para o caso de algo não estar pronto.</param>
public record RespostaSaude(string Api, string Servidor, string BancoDaAplicacao, string? Detalhe);
