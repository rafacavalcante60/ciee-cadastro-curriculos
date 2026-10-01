using CieeCurriculos.Api.Candidatos;
using CieeCurriculos.Api.Dados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CieeCurriculos.Api.Controllers;

/// <summary>
/// Cadastro e consulta de candidatos.
/// </summary>
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

    /// <summary>
    /// Cadastra um candidato. Recebe JSON puro: o currículo em PDF, quando existe,
    /// só serve para preencher o formulário e nunca acompanha o salvamento.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CandidatoResposta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cadastrar(NovoCandidato novo, CancellationToken cancelamento)
    {
        var candidato = new Candidato
        {
            NomeCompleto = novo.NomeCompleto,
            Email = novo.Email.Trim().ToLowerInvariant(),
            Telefone = novo.Telefone,
            AreaOuCargoDeInteresse = novo.AreaOuCargoDeInteresse,
            ResumoProfissional = novo.ResumoProfissional,
            DataCadastro = _relogio.GetUtcNow().UtcDateTime
        };

        _contexto.Candidatos.Add(candidato);
        await _contexto.SaveChangesAsync(cancelamento);

        // A rota de detalhes chega com a fatia da tela de detalhes; o endereço já
        // é o definitivo do contrato da API.
        return Created($"/api/candidatos/{candidato.Id}", CandidatoResposta.De(candidato));
    }

    /// <summary>
    /// Lista os candidatos, dos cadastrados mais recentemente para os mais antigos.
    /// </summary>
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
