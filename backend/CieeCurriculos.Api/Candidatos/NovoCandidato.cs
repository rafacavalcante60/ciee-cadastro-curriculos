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
    string? ResumoProfissional);
