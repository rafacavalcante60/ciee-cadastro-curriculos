using CieeCurriculos.Api.Candidatos;
using CieeCurriculos.Api.Dados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CieeCurriculos.Api.Controllers;

[ApiController]
[Route("api/candidatos")]
public class CandidatosController : ControllerBase
{
    private readonly CurriculosDbContext _contexto;
    private readonly TimeProvider _relogio;

    public CandidatosController(CurriculosDbContext contexto, TimeProvider relogio)
    {
        _contexto = contexto;
        _relogio = relogio;
    }

    // Recebe JSON puro: o PDF só preenche o formulário e nunca acompanha o salvamento.
    [HttpPost]
    [ProducesResponseType(typeof(CandidatoResposta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cadastrar(NovoCandidato novo, CancellationToken cancelamento)
    {
        var candidato = novo.ParaCandidato(dataCadastro: _relogio.GetUtcNow().UtcDateTime);

        if (await _contexto.Candidatos.AnyAsync(c => c.Email == candidato.Email, cancelamento))
        {
            return EmailJaCadastrado();
        }

        // A checagem acima não fecha a corrida entre dois cadastros simultâneos;
        // quem garante a unicidade é o índice do banco.
        _contexto.Candidatos.Add(candidato);
        try
        {
            await _contexto.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException erro) when (ViolouIndiceUnico(erro))
        {
            return EmailJaCadastrado();
        }

        return Created($"/api/candidatos/{candidato.Id}", CandidatoResposta.De(candidato));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CandidatoResposta>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<CandidatoResposta>> Listar(CancellationToken cancelamento)
    {
        var candidatos = await _contexto.Candidatos
            .AsNoTracking()
            .OrderByDescending(c => c.DataCadastro)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancelamento);

        return candidatos.Select(CandidatoResposta.De);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CandidatoResposta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidatoResposta>> Detalhar(int id, CancellationToken cancelamento)
    {
        var candidato = await _contexto.Candidatos
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancelamento);

        if (candidato is null)
        {
            return NotFound();
        }

        return CandidatoResposta.De(candidato);
    }

    private ObjectResult EmailJaCadastrado() => Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "E-mail já cadastrado",
        detail: "Já existe um candidato cadastrado com este e-mail.");

    // 2601 e 2627: códigos do SQL Server para chave duplicada em índice único e em constraint.
    private static bool ViolouIndiceUnico(DbUpdateException erro) =>
        erro.InnerException is SqlException { Number: 2601 or 2627 };
}
