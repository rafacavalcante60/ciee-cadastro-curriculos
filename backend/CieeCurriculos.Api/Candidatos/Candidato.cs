namespace CieeCurriculos.Api.Candidatos;

public class Candidato
{
    public int Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Telefone { get; set; }

    public string? AreaOuCargoDeInteresse { get; set; }

    public string? ResumoProfissional { get; set; }

    // Não pedido pelo enunciado: existe para dar ordem determinística à listagem.
    public DateTime DataCadastro { get; set; }
}
