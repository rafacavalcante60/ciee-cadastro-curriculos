using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CieeCurriculos.Api.Testes.Infraestrutura;

/// <summary>
/// Sobe a aplicação real em memória, apontada para o banco de testes.
/// É o ponto de observação escolhido para os testes do backend: o mais alto
/// disponível, em que as requisições atravessam o pipeline completo —
/// roteamento, validação, controller, Entity Framework e banco.
/// </summary>
public sealed class AplicacaoDeTestes : WebApplicationFactory<Program>
{
    private readonly string _stringDeConexao;
    private readonly TimeProvider? _relogio;

    /// <param name="stringDeConexao">Banco para o qual a aplicação aponta.</param>
    /// <param name="relogio">Substitui o relógio do sistema, quando o teste precisa controlar as datas.</param>
    public AplicacaoDeTestes(string stringDeConexao, TimeProvider? relogio = null)
    {
        _stringDeConexao = stringDeConexao;
        _relogio = relogio;
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

        if (_relogio is not null)
        {
            construtor.ConfigureServices(servicos => servicos.Replace(ServiceDescriptor.Singleton(_relogio)));
        }
    }
}
