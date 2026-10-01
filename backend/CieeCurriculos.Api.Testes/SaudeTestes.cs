using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using CieeCurriculos.Api.Controllers;
using CieeCurriculos.Api.Testes.Infraestrutura;

namespace CieeCurriculos.Api.Testes;

public class SaudeTestes
{
    [Collection(ColecaoDeIntegracao.Nome)]
    public class ComBancoDisponivel
    {
        private readonly BancoDeTestes _banco;

        public ComBancoDisponivel(BancoDeTestes banco)
        {
            _banco = banco;
        }

        [Fact]
        public async Task Confirma_que_a_api_alcanca_o_servidor_de_banco()
        {
            await using var aplicacao = new AplicacaoDeTestes(_banco.StringDeConexao);
            var cliente = aplicacao.CreateClient();

            var resposta = await cliente.GetAsync("/api/saude");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            var corpo = await resposta.Content.ReadFromJsonAsync<RespostaSaude>();
            Assert.NotNull(corpo);
            Assert.Equal("ok", corpo!.Api);
            Assert.Equal("ok", corpo.Servidor);
        }

        [Fact]
        public async Task Relata_banco_da_aplicacao_ausente_quando_o_schema_nao_foi_criado()
        {
            var conexaoParaBancoInexistente = new SqlConnectionStringBuilder(_banco.StringDeConexao)
            {
                InitialCatalog = "CieeCurriculosInexistente"
            }.ConnectionString;

            await using var aplicacao = new AplicacaoDeTestes(conexaoParaBancoInexistente);
            var cliente = aplicacao.CreateClient();

            var resposta = await cliente.GetAsync("/api/saude");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            var corpo = await resposta.Content.ReadFromJsonAsync<RespostaSaude>();
            Assert.NotNull(corpo);
            Assert.Equal("ok", corpo!.Servidor);
            Assert.Equal("ausente", corpo.BancoDaAplicacao);
            Assert.NotNull(corpo.Detalhe);
        }
    }

    public class SemBancoDisponivel
    {
        [Theory]
        [InlineData("isso nao e uma connection string")]
        [InlineData("Server=;Database=;")]
        [InlineData("")]
        public async Task Responde_503_quando_a_connection_string_esta_invalida(string conexao)
        {
            await using var aplicacao = new AplicacaoDeTestes(conexao);
            var cliente = aplicacao.CreateClient();

            var resposta = await cliente.GetAsync("/api/saude");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        }

        [Fact]
        public async Task Responde_503_quando_o_servidor_de_banco_esta_inacessivel()
        {
            await using var aplicacao = new AplicacaoDeTestes(
                "Server=localhost,65000;Database=CieeCurriculos;User Id=sa;Password=NaoImporta1!;"
                + "TrustServerCertificate=True;Connect Timeout=2");
            var cliente = aplicacao.CreateClient();

            var resposta = await cliente.GetAsync("/api/saude");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        }
    }
}
