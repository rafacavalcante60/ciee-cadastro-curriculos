namespace CieeCurriculos.Api.Curriculos;

// Aviso explica por que nada foi lido. Vai direto para a tela, como o detail dos erros.
public record CamposExtraidos(string? NomeCompleto, string? Email, string? Telefone, string? Aviso = null)
{
    public static CamposExtraidos SemResultado(string aviso) => new(null, null, null, aviso);
}
