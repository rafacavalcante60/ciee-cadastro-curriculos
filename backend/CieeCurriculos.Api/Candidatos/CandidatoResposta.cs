namespace CieeCurriculos.Api.Candidatos;

/// <summary>Candidato como a API o devolve.</summary>
public record CandidatoResposta(
    int Id,
    string NomeCompleto,
    string Email,
    string? Telefone,
    string? AreaOuCargoDeInteresse,
    string? ResumoProfissional,
    DateTime DataCadastro)
{
    public static CandidatoResposta De(Candidato candidato) => new(
        candidato.Id,
        candidato.NomeCompleto,
        candidato.Email,
        candidato.Telefone,
        candidato.AreaOuCargoDeInteresse,
        candidato.ResumoProfissional,
        candidato.DataCadastro);
}
