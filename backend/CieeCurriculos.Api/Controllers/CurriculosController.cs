using System.ComponentModel.DataAnnotations;
using CieeCurriculos.Api.Curriculos;
using Microsoft.AspNetCore.Mvc;

namespace CieeCurriculos.Api.Controllers;

[ApiController]
[Route("api/curriculos")]
public class CurriculosController : ControllerBase
{
    private const int LimiteDeBytes = 5 * 1024 * 1024;

    // Todo PDF começa com "%PDF-". Extensão e content-type vêm do cliente e
    // qualquer arquivo pode declará-los.
    private static readonly byte[] AssinaturaDePdf = "%PDF-"u8.ToArray();

    private readonly ILogger<CurriculosController> _logger;

    public CurriculosController(ILogger<CurriculosController> logger)
    {
        _logger = logger;
    }

    // Não cria candidato: devolve um palpite para preencher o formulário.
    //
    // Por padrão o ASP.NET grava em arquivo temporário todo upload acima de 64 KB;
    // o limiar alto mantém o currículo só em memória. O tamanho é conferido aqui e
    // não por MultipartBodyLengthLimit, que recusaria com o erro genérico de
    // validação. O teto do corpo da requisição continua sendo o do Kestrel.
    [HttpPost("extracao")]
    [RequestFormLimits(MemoryBufferThreshold = int.MaxValue)]
    [ProducesResponseType(typeof(CamposExtraidos), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Extrair(
        [Required(ErrorMessage = "Envie um arquivo PDF.")] IFormFile arquivo,
        CancellationToken cancelamento)
    {
        if (arquivo.Length > LimiteDeBytes)
        {
            return ArquivoInvalido("O arquivo excede o limite de 5 MB.");
        }

        using var memoria = new MemoryStream();
        await arquivo.CopyToAsync(memoria, cancelamento);
        var pdf = memoria.ToArray();

        if (!pdf.AsSpan().StartsWith(AssinaturaDePdf))
        {
            return ArquivoInvalido("O arquivo enviado não é um PDF.");
        }

        return Ok(ExtracaoDeCurriculo.Extrair(pdf, _logger));
    }

    private ObjectResult ArquivoInvalido(string detalhe) => Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Arquivo inválido",
        detail: detalhe);
}
