using System.ComponentModel.DataAnnotations;

namespace CieeCurriculos.Api.Candidatos;

// Propriedades em vez de construtor posicional: com parâmetros de construtor, as
// chaves dos erros de validação ignoram o nome JSON configurado no Program.
public record NovoCandidato
{
    [Required(ErrorMessage = "Informe o nome completo.")]
    [MinimoDeCaracteres(2, ErrorMessage = "O nome completo deve ter ao menos 2 caracteres.")]
    [MaxLength(200, ErrorMessage = "Use no máximo {1} caracteres.")]
    public string NomeCompleto { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [FormatoDeEmail(ErrorMessage = "Informe um e-mail válido.")]
    [MaxLength(256, ErrorMessage = "Use no máximo {1} caracteres.")]
    public string Email { get; init; } = string.Empty;

    [FormatoDeTelefone(ErrorMessage = "Informe o telefone com DDD, com 10 ou 11 dígitos.")]
    public string? Telefone { get; init; }

    [MaxLength(120, ErrorMessage = "Use no máximo {1} caracteres.")]
    public string? AreaOuCargoDeInteresse { get; init; }

    [MaxLength(2000, ErrorMessage = "Use no máximo {1} caracteres.")]
    public string? ResumoProfissional { get; init; }

    // E-mail normalizado aqui em vez de depender da collation do banco.
    public Candidato ParaCandidato(DateTime dataCadastro) => new()
    {
        NomeCompleto = NomeCompleto.Trim(),
        Email = Email.Trim().ToLowerInvariant(),
        Telefone = NumeroDeTelefone.Normalizar(Telefone),
        AreaOuCargoDeInteresse = AreaOuCargoDeInteresse,
        ResumoProfissional = ResumoProfissional,
        DataCadastro = dataCadastro
    };
}
