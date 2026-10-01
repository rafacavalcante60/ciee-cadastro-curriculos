using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CieeCurriculos.Api.Candidatos;

// Campo em branco é assunto do [Required]: os atributos abaixo o aceitam, senão o
// campo vazio recebia duas mensagens. O frontend se comporta do mesmo jeito.
// Espaços nas pontas não contam, porque o nome é gravado aparado.
public sealed class MinimoDeCaracteresAttribute : ValidationAttribute
{
    private readonly int _minimo;

    public MinimoDeCaracteresAttribute(int minimo)
    {
        _minimo = minimo;
    }

    public override bool IsValid(object? valor) =>
        valor is not string texto || string.IsNullOrWhiteSpace(texto) || texto.Trim().Length >= _minimo;
}

// Mesma expressão do formulário no frontend. Aceita espaços nas pontas porque o
// e-mail é aparado antes de gravar.
public sealed class FormatoDeEmailAttribute : RegularExpressionAttribute
{
    public FormatoDeEmailAttribute() : base(@"^\s*[^\s@]+@[^\s@]+\.[^\s@]+\s*$")
    {
    }

    public override bool IsValid(object? valor) =>
        string.IsNullOrWhiteSpace(valor as string) || base.IsValid(valor);
}

public sealed class FormatoDeTelefoneAttribute : ValidationAttribute
{
    public override bool IsValid(object? valor) =>
        NumeroDeTelefone.Normalizar(valor as string) is not { } digitos || Regex.IsMatch(digitos, "^[0-9]{10,11}$");
}

public static class NumeroDeTelefone
{
    // Descarta só a máscara e os separadores; qualquer outro caractere sobra e
    // reprova a validação. Mesma regra do formulário no frontend.
    public static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : Regex.Replace(valor, @"[\s().-]", "");
}
