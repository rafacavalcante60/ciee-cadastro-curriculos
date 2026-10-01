using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace CieeCurriculos.Api.Curriculos;

// Falha de leitura vira aviso, não erro: o arquivo é um PDF, então a culpa não é
// do cliente, e o cadastro manual continua possível.
public static class ExtracaoDeCurriculo
{
    public static CamposExtraidos Extrair(byte[] pdf, ILogger logger)
    {
        string texto;
        try
        {
            texto = ExtrairTexto(pdf);
        }
        catch (PdfDocumentEncryptedException)
        {
            return CamposExtraidos.SemResultado("O PDF é protegido por senha e não pôde ser lido. Preencha os campos à mão.");
        }
        // O PdfPig não tem uma exceção única para arquivo malformado: lança também
        // InvalidOperationException, ArgumentException e outras.
        catch (Exception erro)
        {
            logger.LogWarning(erro, "Falha ao ler o texto de um currículo em PDF.");
            return CamposExtraidos.SemResultado("Não foi possível ler o PDF, que pode estar corrompido. Preencha os campos à mão.");
        }

        return ExtracaoDeCampos.Extrair(texto);
    }

    // page.Text junta a página inteira sem quebra de linha, e a heurística do nome
    // trabalha linha a linha. ContentOrderTextExtractor devolve uma linha por linha
    // do documento, na ordem em que o conteúdo foi escrito no arquivo.
    private static string ExtrairTexto(byte[] pdf)
    {
        using var documento = PdfDocument.Open(pdf);
        return string.Join('\n', documento.GetPages().Select(pagina => ContentOrderTextExtractor.GetText(pagina)));
    }
}
