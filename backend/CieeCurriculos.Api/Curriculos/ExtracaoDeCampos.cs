using System.Globalization;
using System.Text.RegularExpressions;

namespace CieeCurriculos.Api.Curriculos;

public static class ExtracaoDeCampos
{
    private static readonly Regex Email = new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}");

    // DDD com ou sem parênteses, número de 8 ou 9 dígitos, separados por espaço,
    // ponto ou hífen. O código do país fica fora do grupo "numero": a validação
    // do cadastro só aceita 10 ou 11 dígitos. As bordas sem dígito evitam pegar
    // um trecho de número maior.
    private static readonly Regex Telefone = new(
        @"(?<!\d)(?:\+?55[\s.-]*)?(?<numero>(?:\(\d{2}\)|\d{2})[\s.-]*\d{4,5}[\s.-]?\d{4})(?!\d)");

    // Um CPF sem pontuação tem 11 dígitos e passaria por telefone: números logo
    // depois do rótulo de um documento são apagados antes de procurar o telefone.
    private static readonly Regex NumeroDeDocumento = new(
        @"\b(?:CPF|CNPJ|CEP|RG)\b[^\d\n]{0,10}\d[\d./-]*", RegexOptions.IgnoreCase);

    private static readonly Regex PalavraCapitalizada = new(@"^\p{Lu}[\p{L}'’-]*$");

    private static readonly HashSet<string> Particulas = ["da", "de", "do", "das", "dos", "e"];

    // Título de documento também tem duas palavras capitalizadas e costuma vir antes do nome.
    private static readonly HashSet<string> PalavrasDeTitulo = ["currículo", "curriculo", "curriculum", "vitae", "resume"];

    private static readonly Regex NomeComRotulo = new(
        @"^nome(?:\s+completo)?\s*[:–-]\s*(?<nome>.+)$", RegexOptions.IgnoreCase);

    public static CamposExtraidos Extrair(string texto)
    {
        var linhas = texto.Split('\n')
            .Select(linha => string.Join(' ', linha.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
            .ToList();
        var email = Email.Match(texto);
        var telefone = Telefone.Match(NumeroDeDocumento.Replace(texto, " "));
        return new CamposExtraidos(
            NomeCompleto: NomeCompleto(linhas),
            Email: email.Success ? email.Value : null,
            Telefone: telefone.Success ? Regex.Replace(telefone.Groups["numero"].Value, @"\D", "") : null);
    }

    private static string? NomeCompleto(List<string> linhas)
    {
        var nome = linhas
            .Select(linha => NomeComRotulo.Match(linha))
            .FirstOrDefault(rotulo => rotulo.Success)?.Groups["nome"].Value.Trim()
            ?? linhas.FirstOrDefault(PareceNome);

        return nome is not null && !nome.Any(char.IsLower) ? IniciaisMaiusculas(nome) : nome;
    }

    private static string IniciaisMaiusculas(string nome)
    {
        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        var palavras = nome.ToLower(cultura).Split(' ').Select((palavra, posicao) =>
            posicao > 0 && Particulas.Contains(palavra) ? palavra : char.ToUpper(palavra[0], cultura) + palavra[1..]);
        return string.Join(' ', palavras);
    }

    // Linhas de e-mail e de telefone caem aqui por terem caracteres que não são letras.
    private static bool PareceNome(string linha)
    {
        var palavras = linha.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return palavras.All(p => Particulas.Contains(p) || PalavraCapitalizada.IsMatch(p))
            && palavras.Count(p => !Particulas.Contains(p)) >= 2
            && !palavras.Any(p => PalavrasDeTitulo.Contains(p.ToLowerInvariant()));
    }
}
