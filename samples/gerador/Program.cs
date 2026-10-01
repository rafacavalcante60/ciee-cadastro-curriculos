using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Os PDFs ficam versionados: o teste lê sempre os mesmos bytes, em vez de
// depender da versão do gerador.
QuestPDF.Settings.License = LicenseType.Community;

var destino = args.Length > 0 ? args[0] : "..";

Gerar("curriculo-completo.pdf", CurriculoCompleto);

Gerar("curriculo-sem-telefone.pdf", pagina => pagina.Content().Column(coluna =>
{
    coluna.Spacing(6);
    coluna.Item().Text("Carlos Eduardo Ferreira").FontSize(22).Bold();
    coluna.Item().Text("Analista de Dados");
    coluna.Item().Text("carlos.ferreira@exemplo.com  ·  linkedin.com/in/carlos-ferreira-exemplo");
    Secao(coluna, "Experiência",
        "Analista de dados júnior, Varejo Exemplo S.A. (2021 – 2023)",
        "Relatórios em Power BI e consultas em SQL para as áreas de vendas e estoque.");
    Secao(coluna, "Formação", "Bacharelado em Estatística, Universidade Exemplo (2016 – 2020)");
}));

Gerar("curriculo-nome-com-rotulo.pdf", pagina => pagina.Content().Column(coluna =>
{
    coluna.Spacing(6);
    coluna.Item().AlignCenter().Text("CURRÍCULO").FontSize(18).Bold();
    coluna.Item().PaddingTop(10).Text("Dados Pessoais").FontSize(13).Bold();
    coluna.Item().Text("Nome completo: JOÃO PEDRO DOS SANTOS");
    coluna.Item().Text("CPF: 12345678901");
    coluna.Item().Text("E-mail: joao.santos@exemplo.com.br");
    coluna.Item().Text("Celular: +55 (21) 99876-5432");
    coluna.Item().Text("Endereço: Rua das Flores, 120, Rio de Janeiro, RJ, CEP 20000000");
    Secao(coluna, "Objetivo", "Vaga de auxiliar administrativo.");
    Secao(coluna, "Experiência", "Assistente de escritório, Comércio Exemplo (2022 – 2025)");
}));

Gerar("curriculo-duas-colunas.pdf", pagina => pagina.Content().Row(linha =>
{
    linha.ConstantItem(170).Background(Colors.Grey.Lighten3).Padding(12).Column(lateral =>
    {
        lateral.Spacing(4);
        lateral.Item().Text("Contato").Bold();
        lateral.Item().Text("ana.oliveira@exemplo.com").FontSize(9);
        lateral.Item().Text("(31) 3333-4444").FontSize(9);
        lateral.Item().Text("Belo Horizonte, MG").FontSize(9);
        lateral.Item().PaddingTop(12).Text("Idiomas").Bold();
        lateral.Item().Text("Inglês avançado").FontSize(9);
        lateral.Item().Text("Espanhol básico").FontSize(9);
    });
    linha.RelativeItem().PaddingLeft(18).Column(principal =>
    {
        principal.Spacing(6);
        principal.Item().Text("Ana Beatriz de Oliveira").FontSize(22).Bold();
        principal.Item().Text("Designer de Interfaces");
        Secao(principal, "Experiência",
            "Designer, Agência Exemplo (2020 – 2025)",
            "Protótipos de aplicativos e sistemas internos, com testes de usabilidade.");
        Secao(principal, "Formação", "Design Gráfico, Escola Exemplo (2016 – 2019)");
    });
}));

// Página do currículo completo transformada em imagem, como sai de um scanner:
// o PDF não tem camada de texto.
var paginaDigitalizada = Curriculo(CurriculoCompleto)
    .GenerateImages(new ImageGenerationSettings { RasterDpi = 150 })
    .Single();
Gerar("curriculo-digitalizado.pdf", pagina =>
{
    pagina.Margin(0);
    pagina.Content().Image(paginaDigitalizada).FitArea();
});

// A criptografia sorteia o sal a cada geração: só este arquivo muda de bytes.
DocumentOperation
    .LoadFile(Path.Combine(destino, "curriculo-completo.pdf"))
    .Encrypt(new DocumentOperation.Encryption256Bit { UserPassword = "senha-do-candidato", OwnerPassword = "senha-do-dono" })
    .Save(Path.Combine(destino, "curriculo-protegido-por-senha.pdf"));

// Começa com a assinatura de PDF, e por isso passa pela checagem de tipo, mas o
// resto é o início de um PDF de verdade cortado no meio.
var completo = File.ReadAllBytes(Path.Combine(destino, "curriculo-completo.pdf"));
File.WriteAllBytes(Path.Combine(destino, "curriculo-corrompido.pdf"), completo[..(completo.Length / 3)]);

// Texto puro com extensão .pdf: a checagem da API olha os bytes, não o nome.
File.WriteAllText(Path.Combine(destino, "nao-e-pdf.pdf"),
    "Este arquivo é texto puro com extensão .pdf, para testar a checagem de assinatura de bytes.\n");

void Gerar(string arquivo, Action<PageDescriptor> conteudo) =>
    Curriculo(conteudo).GeneratePdf(Path.Combine(destino, arquivo));

static IDocument Curriculo(Action<PageDescriptor> conteudo) =>
    Document.Create(documento => documento.Page(pagina =>
    {
        pagina.Size(PageSizes.A4);
        pagina.Margin(2, Unit.Centimetre);
        pagina.DefaultTextStyle(estilo => estilo.FontSize(11));
        conteudo(pagina);
    }))
    .WithMetadata(new DocumentMetadata { CreationDate = DateTimeOffset.UnixEpoch, ModifiedDate = DateTimeOffset.UnixEpoch });

static void CurriculoCompleto(PageDescriptor pagina) => pagina.Content().Column(coluna =>
{
    coluna.Spacing(6);
    coluna.Item().Text("Maria Aparecida da Silva").FontSize(22).Bold();
    coluna.Item().Text("Desenvolvedora de Software Júnior").FontSize(13);
    coluna.Item().Text("maria.silva@exemplo.com  ·  (11) 98765-4321  ·  São Paulo, SP");
    Secao(coluna, "Resumo profissional",
        "Estudante de Análise e Desenvolvimento de Sistemas, com experiência em C#, .NET e Angular. " +
        "Interesse em desenvolvimento web e em boas práticas de teste.");
    Secao(coluna, "Experiência",
        "Estagiária de desenvolvimento, Empresa Fictícia Ltda. (2024 – 2025)",
        "Manutenção de APIs em ASP.NET Core e telas em Angular.");
    Secao(coluna, "Formação",
        "Tecnologia em Análise e Desenvolvimento de Sistemas, Faculdade Exemplo (2023 – 2026)");
    Secao(coluna, "Habilidades", "C#, .NET, SQL Server, Angular, TypeScript, Git");
});

static void Secao(ColumnDescriptor coluna, string titulo, params string[] paragrafos)
{
    coluna.Item().PaddingTop(10).Text(titulo).FontSize(13).Bold();
    foreach (var paragrafo in paragrafos)
    {
        coluna.Item().Text(paragrafo);
    }
}
