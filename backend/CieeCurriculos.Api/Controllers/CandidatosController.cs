using CieeCurriculos.Api.Candidatos;
using CieeCurriculos.Api.Dados;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Cadastrar(NovoCandidato novo, CancellationToken cancelamento)
    {
        var candidato = novo.ParaCandidato(dataCadastro: _relogio.GetUtcNow().UtcDateTime);

        _contexto.Candidatos.Add(candidato);
        await _contexto.SaveChangesAsync(cancelamento);

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
}
