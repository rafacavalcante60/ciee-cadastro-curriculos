using System.Net;
using System.Net.Http.Json;
using CieeCurriculos.Api.Candidatos;
using CieeCurriculos.Api.Testes.Infraestrutura;
using Microsoft.AspNetCore.Mvc;

namespace CieeCurriculos.Api.Testes;

[Collection(ColecaoDeIntegracao.Nome)]
public class CandidatosTestes : IAsyncLifetime
{
    private readonly BancoDeTestes _banco;
    private readonly RelogioDeTestes _relogio = new(new DateTimeOffset(2026, 3, 10, 14, 30, 0, TimeSpan.Zero));
    private readonly AplicacaoDeTestes _aplicacao;
    private readonly HttpClient _cliente;

    public CandidatosTestes(BancoDeTestes banco)
    {
        _banco = banco;
        _aplicacao = new AplicacaoDeTestes(banco.StringDeConexao, _relogio);
        _cliente = _aplicacao.CreateClient();
    }

    public Task InitializeAsync() => _banco.LimparCandidatosAsync();

    public async Task DisposeAsync() => await _aplicacao.DisposeAsync();

    [Fact]
    public async Task Candidato_cadastrado_aparece_na_listagem()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new
        {
            nomeCompleto = "Maria da Silva",
            email = "maria.silva@exemplo.com",
            telefone = "11987654321",
            areaOuCargoDeInteresse = "Desenvolvimento de software",
            resumoProfissional = "Estudante de Análise e Desenvolvimento de Sistemas."
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<CandidatoResposta>();
        Assert.NotNull(criado);
        Assert.Equal($"/api/candidatos/{criado!.Id}", resposta.Headers.Location?.OriginalString);

        var lista = await ListarAsync();

        var listado = Assert.Single(lista);
        Assert.Equal(criado.Id, listado.Id);
        Assert.Equal("Maria da Silva", listado.NomeCompleto);
        Assert.Equal("maria.silva@exemplo.com", listado.Email);
        Assert.Equal("11987654321", listado.Telefone);
        Assert.Equal("Desenvolvimento de software", listado.AreaOuCargoDeInteresse);
        Assert.Equal("Estudante de Análise e Desenvolvimento de Sistemas.", listado.ResumoProfissional);
    }

    [Fact]
    public async Task Listagem_traz_os_mais_recentes_primeiro_pela_data_de_cadastro()
    {
        // Cadastrados fora da ordem cronológica: o mais recente recebe o menor Id.
        // Assim a ordem esperada só sai certa se a listagem ordenar pela data,
        // e não pela ordem de inserção.
        _relogio.Agora = new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);
        await CadastrarAsync("Candidata de março", "marco@exemplo.com");

        _relogio.Agora = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);
        await CadastrarAsync("Candidata de janeiro", "janeiro@exemplo.com");

        _relogio.Agora = new DateTimeOffset(2026, 2, 20, 9, 0, 0, TimeSpan.Zero);
        await CadastrarAsync("Candidata de fevereiro", "fevereiro@exemplo.com");

        var lista = await ListarAsync();

        Assert.Equal(
            new[] { "Candidata de março", "Candidata de fevereiro", "Candidata de janeiro" },
            lista.Select(candidato => candidato.NomeCompleto));
    }

    [Fact]
    public async Task Listagem_vazia_devolve_lista_vazia()
    {
        var lista = await ListarAsync();

        Assert.Empty(lista);
    }

    [Fact]
    public async Task Data_de_cadastro_e_gerada_no_servidor_e_ignora_a_enviada_pelo_cliente()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new
        {
            nomeCompleto = "João Pereira",
            email = "joao@exemplo.com",
            dataCadastro = "2000-01-01T00:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var lista = await ListarAsync();
        Assert.Equal(new DateTime(2026, 3, 10, 14, 30, 0, DateTimeKind.Utc), Assert.Single(lista).DataCadastro);
    }

    [Fact]
    public async Task Email_e_gravado_em_minusculas_e_sem_espacos_nas_pontas()
    {
        await CadastrarAsync("Ana Souza", "  Ana.Souza@Exemplo.COM ");

        var lista = await ListarAsync();

        Assert.Equal("ana.souza@exemplo.com", Assert.Single(lista).Email);
    }

    [Fact]
    public async Task Detalhes_de_candidato_existente_devolvem_os_dados_gravados()
    {
        var resumoLongo = string.Concat(Enumerable.Repeat("Experiência com atendimento e rotinas administrativas. ", 30));
        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new
        {
            nomeCompleto = "Maria da Silva",
            email = "maria.silva@exemplo.com",
            telefone = "11987654321",
            areaOuCargoDeInteresse = "Desenvolvimento de software",
            resumoProfissional = resumoLongo
        });
        var criado = (await resposta.Content.ReadFromJsonAsync<CandidatoResposta>())!;

        var detalhes = await _cliente.GetAsync($"/api/candidatos/{criado.Id}");

        Assert.Equal(HttpStatusCode.OK, detalhes.StatusCode);
        var candidato = (await detalhes.Content.ReadFromJsonAsync<CandidatoResposta>())!;
        Assert.Equal(criado.Id, candidato.Id);
        Assert.Equal("Maria da Silva", candidato.NomeCompleto);
        Assert.Equal("maria.silva@exemplo.com", candidato.Email);
        Assert.Equal("11987654321", candidato.Telefone);
        Assert.Equal("Desenvolvimento de software", candidato.AreaOuCargoDeInteresse);
        Assert.Equal(resumoLongo, candidato.ResumoProfissional);
        Assert.Equal(new DateTime(2026, 3, 10, 14, 30, 0, DateTimeKind.Utc), candidato.DataCadastro);
    }

    [Fact]
    public async Task Detalhes_de_identificador_inexistente_devolvem_404_em_ProblemDetails()
    {
        var resposta = await _cliente.GetAsync("/api/candidatos/999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problema?.Status);
    }

    private async Task<List<CandidatoResposta>> ListarAsync() =>
        (await _cliente.GetFromJsonAsync<List<CandidatoResposta>>("/api/candidatos"))!;

    private async Task CadastrarAsync(string nomeCompleto, string email)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/candidatos", new { nomeCompleto, email });
        resposta.EnsureSuccessStatusCode();
    }
}
