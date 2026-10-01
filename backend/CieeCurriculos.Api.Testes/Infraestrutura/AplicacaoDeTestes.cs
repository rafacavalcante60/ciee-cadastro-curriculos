using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CieeCurriculos.Api.Testes.Infraestrutura;

/// <summary>
/// Sobe a aplicação real em memória, apontada para o banco de testes.
/// Este é o seam de teste do backend: as requisições atravessam o pipeline
/// completo — roteamento, validação, controller, Entity Framework e banco.
/// </summary>
public sealed class AplicacaoDeTestes : WebApplicationFactory<Program>
{
    private readonly string _stringDeConexao;

    public AplicacaoDeTestes(string stringDeConexao)
    {
        _stringDeConexao = stringDeConexao;
    }

    protected override void ConfigureWebHost(IWebHostBuilder construtor)
    {
        construtor.UseEnvironment("Testing");

        construtor.ConfigureAppConfiguration((_, configuracao) =>
        {
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CurriculosDb"] = _stringDeConexao
            });
        });
    }
}
