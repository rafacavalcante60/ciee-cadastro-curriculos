namespace CieeCurriculos.Api.Curriculos;

// Os avisos vão direto para a tela, como o detail dos erros.
public record CamposExtraidos(string? NomeCompleto, string? Email, string? Telefone, IReadOnlyList<string> Avisos)
{
    public static CamposExtraidos SemResultado(string aviso) => new(null, null, null, [aviso]);
}
