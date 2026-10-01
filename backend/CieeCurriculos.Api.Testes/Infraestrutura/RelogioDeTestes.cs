namespace CieeCurriculos.Api.Testes.Infraestrutura;

/// <summary>
/// Relógio controlado pelo teste, para que datas geradas no servidor tenham
/// valor conhecido de antemão em vez de depender do instante da execução.
/// </summary>
public sealed class RelogioDeTestes : TimeProvider
{
    public RelogioDeTestes(DateTimeOffset agora)
    {
        Agora = agora;
    }

    public DateTimeOffset Agora { get; set; }

    public override DateTimeOffset GetUtcNow() => Agora;
}
