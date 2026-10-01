using System.Net;
using System.Net.Http.Json;
using CieeCurriculos.Api.Candidatos;
using CieeCurriculos.Api.Testes.Infraestrutura;
using Microsoft.AspNetCore.Mvc;

namespace CieeCurriculos.Api.Testes;

[Collection(ColecaoDeIntegracao.Nome)]
public class ValidacaoDoCadastroTestes : IAsyncLifetime
{
    private readonly BancoDeTestes _banco;
    private readonly AplicacaoDeTestes _aplicacao;
    private readonly HttpClient _cliente;

    public ValidacaoDoCadastroTestes(BancoDeTestes banco)
    {
        _banco = banco;
        _aplicacao = new AplicacaoDeTestes(banco.StringDeConexao);
        _cliente = _aplicacao.CreateClient();
    }

    public Task InitializeAsync() => _banco.LimparCandidatosAsync();

    public async Task DisposeAsync() => await _aplicacao.DisposeAsync();

    [Theory]
    [InlineData(null, "Informe o nome completo.")]
    [InlineData("", "Informe o nome completo.")]
    [InlineData("   ", "Informe o nome completo.")]
    [InlineData("M", "O nome completo deve ter ao menos 2 caracteres.")]
    public async Task Nome_ausente_ou_curto_devolve_400_no_campo_do_nome(string? nomeCompleto, string mensagem)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new { nomeCompleto, email = "maria@exemplo.com" });

        var erros = await ErrosDeValidacaoAsync(resposta);
        Assert.Equal(new[] { "nomeCompleto" }, erros.Keys);
        Assert.Equal(new[] { mensagem }, erros["nomeCompleto"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Email_ausente_devolve_400_no_campo_do_email(string? email)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new { nomeCompleto = "Maria da Silva", email });

        var erros = await ErrosDeValidacaoAsync(resposta);
        Assert.Equal(new[] { "email" }, erros.Keys);
        Assert.Equal(new[] { "Informe o e-mail." }, erros["email"]);
    }

    [Theory]
    [InlineData("maria")]
    [InlineData("maria@")]
    [InlineData("@exemplo.com")]
    [InlineData("maria@exemplo")]
    [InlineData("maria silva@exemplo.com")]
    [InlineData("maria@@exemplo.com")]
    public async Task Email_em_formato_invalido_devolve_400_no_campo_do_email(string email)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new { nomeCompleto = "Maria da Silva", email });

        var erros = await ErrosDeValidacaoAsync(resposta);
        Assert.Equal(new[] { "email" }, erros.Keys);
        Assert.Equal(new[] { "Informe um e-mail válido." }, erros["email"]);
    }

    [Theory]
    [InlineData("119876543")]
    [InlineData("(11) 9876-543")]
    [InlineData("119876543210")]
    [InlineData("+55 11 98765-4321")]
    [InlineData("11 98765-432a")]
    public async Task Telefone_sem_10_ou_11_digitos_devolve_400_no_campo_do_telefone(string telefone)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new
        {
            nomeCompleto = "Maria da Silva",
            email = "maria@exemplo.com",
            telefone
        });

        var erros = await ErrosDeValidacaoAsync(resposta);
        Assert.Equal(new[] { "telefone" }, erros.Keys);
        Assert.Equal(new[] { "Informe o telefone com DDD, com 10 ou 11 dígitos." }, erros["telefone"]);
    }

    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("11 98765 4321", "11987654321")]
    [InlineData("11.3333.4444", "1133334444")]
    [InlineData("1133334444", "1133334444")]
    public async Task Telefone_com_ou_sem_mascara_e_aceito_e_gravado_so_com_digitos(string telefone, string gravado)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new
        {
            nomeCompleto = "Maria da Silva",
            email = "maria@exemplo.com",
            telefone
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<CandidatoResposta>();
        Assert.Equal(gravado, criado!.Telefone);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Telefone_e_campos_opcionais_vazios_sao_aceitos(string? vazio)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new
        {
            nomeCompleto = "Maria da Silva",
            email = "maria@exemplo.com",
            telefone = vazio,
            areaOuCargoDeInteresse = vazio,
            resumoProfissional = vazio
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Theory]
    [InlineData("nomeCompleto", 200)]
    [InlineData("areaOuCargoDeInteresse", 120)]
    [InlineData("resumoProfissional", 2000)]
    public async Task Texto_acima_do_tamanho_do_campo_devolve_400_em_vez_de_estourar_no_banco(string campo, int maximo)
    {
        var corpo = new Dictionary<string, string>
        {
            ["nomeCompleto"] = "Maria da Silva",
            ["email"] = "maria@exemplo.com",
            [campo] = new string('a', maximo + 1)
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", corpo);

        var erros = await ErrosDeValidacaoAsync(resposta);
        Assert.Equal(new[] { campo }, erros.Keys);
        Assert.Equal(new[] { $"Use no máximo {maximo} caracteres." }, erros[campo]);
    }

    [Fact]
    public async Task Email_acima_de_256_caracteres_devolve_400()
    {
        var email = new string('a', 245) + "@exemplo.com";

        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new { nomeCompleto = "Maria da Silva", email });

        var erros = await ErrosDeValidacaoAsync(resposta);
        Assert.Equal(new[] { "Use no máximo 256 caracteres." }, erros["email"]);
    }

    [Theory]
    [InlineData("maria@exemplo.com")]
    [InlineData("  Maria@Exemplo.COM ")]
    public async Task Email_ja_cadastrado_devolve_409_com_mensagem_especifica(string emailRepetido)
    {
        var primeiro = await _cliente.PostAsJsonAsync("/api/candidatos", new { nomeCompleto = "Maria da Silva", email = "maria@exemplo.com" });
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);

        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new { nomeCompleto = "Maria Souza", email = emailRepetido });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problema!.Status);
        Assert.Equal("Já existe um candidato cadastrado com este e-mail.", problema.Detail);
    }

    [Fact]
    public async Task Cadastros_simultaneos_com_o_mesmo_email_gravam_um_e_recusam_os_demais_com_409()
    {
        // Disparados juntos, vários passam pela checagem antes de qualquer um gravar;
        // quem barra os demais é o índice único do banco.
        var envios = Enumerable.Range(1, 10).Select(_ =>
            _cliente.PostAsJsonAsync("/api/candidatos", new { nomeCompleto = "Maria da Silva", email = "maria@exemplo.com" }));

        var respostas = await Task.WhenAll(envios);

        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(respostas.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    private static async Task<IDictionary<string, string[]>> ErrosDeValidacaoAsync(HttpResponseMessage resposta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        return problema!.Errors;
    }
}
