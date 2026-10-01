namespace CieeCurriculos.Api.Candidatos;

public record NovoCandidato(
    string NomeCompleto,
    string Email,
    string? Telefone,
    string? AreaOuCargoDeInteresse,
    string? ResumoProfissional)
{
    // E-mail normalizado aqui em vez de depender da collation do banco.
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
