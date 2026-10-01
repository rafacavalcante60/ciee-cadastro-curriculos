namespace CieeCurriculos.Api.Candidatos;

/// <summary>
/// Dados enviados pelo formulário de cadastro. Não traz <c>Id</c> nem
/// <c>DataCadastro</c>: os dois são gerados no servidor, e um valor enviado
/// pelo cliente para eles é simplesmente ignorado.
/// </summary>
public record NovoCandidato(
    string NomeCompleto,
    string Email,
    string? Telefone,
    string? AreaOuCargoDeInteresse,
    string? ResumoProfissional)
{
    /// <summary>
    /// Monta o candidato a gravar, com o e-mail normalizado para minúsculas e sem
    /// espaços nas pontas, em vez de depender da collation do banco.
    /// </summary>
    public Candidato ParaCandidato(DateTime dataCadastro) => new()
    {
        NomeCompleto = NomeCompleto,
        Email = Email.Trim().ToLowerInvariant(),
        Telefone = Telefone,
        AreaOuCargoDeInteresse = AreaOuCargoDeInteresse,
        ResumoProfissional = ResumoProfissional,
        DataCadastro = dataCadastro
    };
}
