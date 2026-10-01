using Testcontainers.MsSql;

namespace CieeCurriculos.Api.Testes.Infraestrutura;

/// <summary>
/// Fornece um SQL Server real para os testes de integração.
/// Quando a variável de ambiente TEST_SQL_CONNECTION está definida, usa aquele banco;
/// caso contrário sobe um container dedicado. Isso mantém um só caminho de código
/// entre a máquina do desenvolvedor e a integração contínua.
/// </summary>
public sealed class BancoDeTestes : IAsyncLifetime
{
    public const string VariavelDeAmbiente = "TEST_SQL_CONNECTION";

    private MsSqlContainer? _container;

    public string StringDeConexao { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var conexaoExterna = Environment.GetEnvironmentVariable(VariavelDeAmbiente);

        if (!string.IsNullOrWhiteSpace(conexaoExterna))
        {
            StringDeConexao = conexaoExterna;
            return;
        }

        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await _container.StartAsync();
        StringDeConexao = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Compartilha um único banco entre todas as classes de teste de integração,
/// para que o container suba uma vez por execução da suíte.
/// </summary>
[CollectionDefinition(Nome)]
public class ColecaoDeIntegracao : ICollectionFixture<BancoDeTestes>
{
    public const string Nome = "Integração";
}
