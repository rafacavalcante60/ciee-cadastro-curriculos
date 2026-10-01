namespace CieeCurriculos.Api.Candidatos;

/// <summary>
/// Pessoa cadastrada para ser considerada em processos seletivos.
/// É a única entidade persistida pela aplicação.
/// </summary>
public class Candidato
{
    public int Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    /// <summary>Gravado em minúsculas e sem espaços nas pontas.</summary>
    public string Email { get; set; } = string.Empty;

    public string? Telefone { get; set; }

    public string? AreaOuCargoDeInteresse { get; set; }

    public string? ResumoProfissional { get; set; }

    /// <summary>
    /// Instante do cadastro, em UTC, gerado no servidor. Não foi pedido pelo
    /// enunciado, mas é o que dá ordem determinística à listagem.
    /// </summary>
    public DateTime DataCadastro { get; set; }
}
