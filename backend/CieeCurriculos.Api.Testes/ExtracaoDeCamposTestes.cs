using CieeCurriculos.Api.Curriculos;

namespace CieeCurriculos.Api.Testes;

public class ExtracaoDeCamposTestes
{
    [Fact]
    public void Curriculo_completo_produz_os_tres_campos()
    {
        var campos = ExtracaoDeCampos.Extrair("""
            Maria Aparecida da Silva
            Desenvolvedora de Software
            maria.silva@exemplo.com | (11) 98765-4321
            """);

        Assert.Equal(new CamposExtraidos("Maria Aparecida da Silva", "maria.silva@exemplo.com", "11987654321"), campos);
    }

    [Theory]
    [InlineData("Maria Aparecida da Silva\nmaria.silva@exemplo.com", "Maria Aparecida da Silva", "maria.silva@exemplo.com", null)]
    [InlineData("Maria Aparecida da Silva\n(11) 98765-4321", "Maria Aparecida da Silva", null, "11987654321")]
    [InlineData("maria.silva@exemplo.com\n(11) 98765-4321", null, "maria.silva@exemplo.com", "11987654321")]
    [InlineData("maria.silva@exemplo.com", null, "maria.silva@exemplo.com", null)]
    [InlineData("experiência em atendimento ao cliente", null, null, null)]
    public void Campo_nao_identificado_volta_nulo(string texto, string? nome, string? email, string? telefone)
    {
        var campos = ExtracaoDeCampos.Extrair(texto);

        Assert.Equal(new CamposExtraidos(nome, email, telefone), campos);
    }

    // É o que sai de um PDF digitalizado: a página é uma imagem.
    [Theory]
    [InlineData("")]
    [InlineData(" \n\n  \n")]
    public void Texto_vazio_produz_todos_os_campos_nulos_e_um_aviso(string texto)
    {
        var campos = ExtracaoDeCampos.Extrair(texto);

        Assert.Equal(new CamposExtraidos(null, null, null,
            "O PDF não tem texto selecionável, como acontece com currículos digitalizados. Preencha os campos à mão."), campos);
    }

    [Fact]
    public void Identifica_o_email()
    {
        var campos = ExtracaoDeCampos.Extrair("""
            Maria da Silva
            Contato: maria.silva@exemplo.com.br
            """);

        Assert.Equal("maria.silva@exemplo.com.br", campos.Email);
    }

    [Fact]
    public void Identifica_o_nome_na_primeira_linha_com_duas_palavras_capitalizadas()
    {
        var campos = ExtracaoDeCampos.Extrair("""
            Currículo
            maria.silva@exemplo.com
            (11) 98765-4321
            Maria Aparecida da Silva
            Desenvolvedora de Software
            """);

        Assert.Equal("Maria Aparecida da Silva", campos.NomeCompleto);
    }

    [Theory]
    [InlineData("Curriculum Vitae")]
    [InlineData("Currículo Profissional")]
    [InlineData("Rua das Flores, 120")]
    [InlineData("Objetivo: Estágio")]
    public void Linha_que_nao_e_nome_e_ignorada(string linha)
    {
        var campos = ExtracaoDeCampos.Extrair($"""
            {linha}
            Maria Aparecida da Silva
            """);

        Assert.Equal("Maria Aparecida da Silva", campos.NomeCompleto);
    }

    [Theory]
    [InlineData("Nome: João Pedro dos Santos")]
    [InlineData("Nome completo: João Pedro dos Santos")]
    [InlineData("NOME COMPLETO - João Pedro dos Santos")]
    public void Nome_com_rotulo_tem_prioridade_sobre_a_posicao(string linhaDoNome)
    {
        var campos = ExtracaoDeCampos.Extrair($"""
            Desenvolvedor Backend
            {linhaDoNome}
            """);

        Assert.Equal("João Pedro dos Santos", campos.NomeCompleto);
    }

    [Theory]
    [InlineData("MARIA APARECIDA DA SILVA", "Maria Aparecida da Silva")]
    [InlineData("JOÃO PEDRO DOS SANTOS E SOUZA", "João Pedro dos Santos e Souza")]
    [InlineData("Nome: ANA DE OLIVEIRA", "Ana de Oliveira")]
    [InlineData("MARIA-JOSÉ D'ÁVILA", "Maria-José D'Ávila")]
    public void Nome_em_maiusculas_ganha_iniciais_maiusculas_com_particulas_minusculas(string linha, string esperado)
    {
        var campos = ExtracaoDeCampos.Extrair(linha);

        Assert.Equal(esperado, campos.NomeCompleto);
    }

    [Fact]
    public void Nome_com_maiusculas_e_minusculas_e_mantido_como_escrito()
    {
        var campos = ExtracaoDeCampos.Extrair("Maria McDonald da Silva");

        Assert.Equal("Maria McDonald da Silva", campos.NomeCompleto);
    }

    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("11 98765-4321", "11987654321")]
    [InlineData("11 98765 4321", "11987654321")]
    [InlineData("11.98765.4321", "11987654321")]
    [InlineData("11987654321", "11987654321")]
    [InlineData("(21) 3333-4444", "2133334444")]
    [InlineData("(55) 99876-5432", "55998765432")]
    [InlineData("+55 11 98765-4321", "11987654321")]
    [InlineData("+55 (11) 98765-4321", "11987654321")]
    [InlineData("+5511987654321", "11987654321")]
    [InlineData("55 11 98765-4321", "11987654321")]
    public void Identifica_o_telefone_em_formatos_brasileiros(string escrito, string esperado)
    {
        var campos = ExtracaoDeCampos.Extrair($"""
            Maria da Silva
            Telefone: {escrito}
            """);

        Assert.Equal(esperado, campos.Telefone);
    }

    [Theory]
    [InlineData("CPF: 12345678901")]
    [InlineData("CPF 123.456.789-01")]
    [InlineData("cpf nº 12345678901")]
    [InlineData("RG: 1234567890")]
    [InlineData("CNPJ: 12345678000190")]
    [InlineData("CEP: 0131010000")]
    public void Numero_com_rotulo_de_documento_nao_vira_telefone(string documento)
    {
        var campos = ExtracaoDeCampos.Extrair($"""
            Maria da Silva
            {documento}
            """);

        Assert.Null(campos.Telefone);
    }

    [Theory]
    [InlineData("RG — Tel: 11 98765-4321")]
    [InlineData("CEP e telefone: 11 98765-4321")]
    public void Rotulo_de_documento_sem_numero_nao_apaga_o_telefone_seguinte(string linha)
    {
        var campos = ExtracaoDeCampos.Extrair(linha);

        Assert.Equal("11987654321", campos.Telefone);
    }

    [Fact]
    public void Telefone_e_identificado_mesmo_com_cpf_antes_dele()
    {
        var campos = ExtracaoDeCampos.Extrair("""
            Maria da Silva
            CPF: 12345678901 Telefone: (11) 98765-4321
            """);

        Assert.Equal("11987654321", campos.Telefone);
    }
}
