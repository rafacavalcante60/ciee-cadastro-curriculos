using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CieeCurriculos.Api.Candidatos;
using CieeCurriculos.Api.Curriculos;
using CieeCurriculos.Api.Testes.Infraestrutura;
using Microsoft.AspNetCore.Mvc;

namespace CieeCurriculos.Api.Testes;

[Collection(ColecaoDeIntegracao.Nome)]
public class ExtracaoDeCurriculoTestes : IAsyncLifetime
{
    private readonly BancoDeTestes _banco;
    private readonly AplicacaoDeTestes _aplicacao;
    private readonly HttpClient _cliente;

    public ExtracaoDeCurriculoTestes(BancoDeTestes banco)
    {
        _banco = banco;
        _aplicacao = new AplicacaoDeTestes(banco.StringDeConexao);
        _cliente = _aplicacao.CreateClient();
    }

    public Task InitializeAsync() => _banco.LimparCandidatosAsync();

    public async Task DisposeAsync() => await _aplicacao.DisposeAsync();

    [Fact]
    public async Task Pdf_completo_devolve_os_tres_campos_identificados()
    {
        var resposta = await EnviarAsync("curriculo-completo.pdf", Amostra("curriculo-completo.pdf"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var campos = await resposta.Content.ReadFromJsonAsync<CamposExtraidos>();
        Assert.Equal(new CamposExtraidos("Maria Aparecida da Silva", "maria.silva@exemplo.com", "11987654321"), campos);
    }

    [Fact]
    public async Task Extracao_nao_cria_candidato_e_seus_campos_concluem_o_cadastro()
    {
        var extracao = await EnviarAsync("curriculo-completo.pdf", Amostra("curriculo-completo.pdf"));
        var campos = await extracao.Content.ReadFromJsonAsync<CamposExtraidos>();

        Assert.Empty((await _cliente.GetFromJsonAsync<List<CandidatoResposta>>("/api/candidatos"))!);

        var cadastro = await _cliente.PostAsJsonAsync("/api/candidatos", new NovoCandidato
        {
            NomeCompleto = campos!.NomeCompleto!,
            Email = campos.Email!,
            Telefone = campos.Telefone
        });
        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);
    }

    [Fact]
    public async Task Arquivo_com_extensao_pdf_que_nao_e_pdf_devolve_400()
    {
        var resposta = await EnviarAsync(ArquivoQueNaoEPdf, Amostra(ArquivoQueNaoEPdf));

        var problema = await ProblemaAsync(resposta);
        Assert.Equal("Arquivo inválido", problema.Title);
        Assert.Equal("O arquivo enviado não é um PDF.", problema.Detail);
    }

    // Gerado aqui para não versionar um binário de 5 MB. Começa com a assinatura de
    // PDF para que só o tamanho o reprove.
    [Theory]
    [InlineData(5 * 1024 * 1024 + 1)]
    [InlineData(12 * 1024 * 1024)]
    public async Task Arquivo_acima_de_5_mb_devolve_400(int tamanho)
    {
        var conteudo = new byte[tamanho];
        "%PDF-1.7\n"u8.CopyTo(conteudo);

        var resposta = await EnviarAsync("grande.pdf", conteudo);

        var problema = await ProblemaAsync(resposta);
        Assert.Equal("Arquivo inválido", problema.Title);
        Assert.Equal("O arquivo excede o limite de 5 MB.", problema.Detail);
    }

    [Fact]
    public async Task Requisicao_sem_arquivo_devolve_400_no_campo_do_arquivo()
    {
        var resposta = await _cliente.PostAsync("/api/curriculos/extracao", new MultipartFormDataContent { { new StringContent("x"), "outro" } });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(new[] { "Envie um arquivo PDF." }, problema!.Errors["arquivo"]);
    }

    public static IEnumerable<object[]> Amostras() => EsperadoDasAmostras().Keys.Select(arquivo => new object[] { arquivo });

    [Theory]
    [MemberData(nameof(Amostras))]
    public async Task Cada_amostra_produz_os_campos_declarados_em_esperado_json(string arquivo)
    {
        var resposta = await EnviarAsync(arquivo, Amostra(arquivo));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(EsperadoDasAmostras()[arquivo], await resposta.Content.ReadFromJsonAsync<CamposExtraidos>());
    }

    // Todo PDF de samples/ precisa estar em esperado.json: amostra nova sem resultado
    // declarado ficaria fora do teste sem ninguém perceber.
    [Fact]
    public void Todo_pdf_de_samples_tem_resultado_declarado()
    {
        var pdfs = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "samples"), "*.pdf")
            .Select(Path.GetFileName)
            .Where(arquivo => arquivo != ArquivoQueNaoEPdf);

        Assert.Equal(pdfs.Order(), EsperadoDasAmostras().Keys.Order());
    }

    private const string ArquivoQueNaoEPdf = "nao-e-pdf.pdf";

    private static Dictionary<string, CamposExtraidos> EsperadoDasAmostras() =>
        JsonSerializer.Deserialize<Dictionary<string, CamposExtraidos>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "samples", "esperado.json")),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    private static async Task<ProblemDetails> ProblemaAsync(HttpResponseMessage resposta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        return (await resposta.Content.ReadFromJsonAsync<ProblemDetails>())!;
    }

    private static byte[] Amostra(string arquivo) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "samples", arquivo));

    private Task<HttpResponseMessage> EnviarAsync(string nomeDoArquivo, byte[] conteudo)
    {
        var formulario = new MultipartFormDataContent
        {
            { new ByteArrayContent(conteudo), "arquivo", nomeDoArquivo }
        };
        return _cliente.PostAsync("/api/curriculos/extracao", formulario);
    }
}
