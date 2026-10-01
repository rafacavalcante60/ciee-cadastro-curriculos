using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace CieeCurriculos.Api.Curriculos;

public static class LeitorDePdf
{
    // page.Text junta a página inteira sem quebra de linha, e a heurística do nome
    // trabalha linha a linha. ContentOrderTextExtractor devolve uma linha por linha
    // do documento, na ordem em que o conteúdo foi escrito no arquivo.
    public static string ExtrairTexto(byte[] pdf)
    {
        using var documento = PdfDocument.Open(pdf);
        return string.Join('\n', documento.GetPages().Select(pagina => ContentOrderTextExtractor.GetText(pagina)));
    }
}
