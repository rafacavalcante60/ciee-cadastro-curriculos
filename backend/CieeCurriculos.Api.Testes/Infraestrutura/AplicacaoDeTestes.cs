using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CieeCurriculos.Api.Testes.Infraestrutura;

public sealed class AplicacaoDeTestes : WebApplicationFactory<Program>
{
    private readonly string _stringDeConexao;
    private readonly TimeProvider? _relogio;

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
