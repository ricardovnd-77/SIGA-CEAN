using System.Globalization;
using SigaCean.Models;
using SigaCean.Views;

namespace SigaCean.Tests;

// Testa SÓ os textos que as telas mostram (Views/Rotulos.cs). Aqui não há regra de estoque.
// Linhas.cs e as telas não são testadas: usam MAUI e não compilam no projeto de testes.
//
// A cultura é fixada em "invariante" durante cada teste para o resultado não depender do idioma do Windows
// de quem roda (o formato de data dd/MM/yyyy e a leitura de números usam a cultura atual).
public class RotulosTests : IDisposable
{
    private readonly CultureInfo _culturaOriginal = CultureInfo.CurrentCulture;

    public RotulosTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culturaOriginal;
    }

    // ---------- Nomes dos enums ----------

    [Theory]
    [InlineData(Categoria.Medicamento, "Medicamento")]
    [InlineData(Categoria.Insumo, "Insumo")]
    [InlineData(Categoria.Alimentar, "Alimentar")]
    [InlineData(Categoria.EquipamentoHospitalar, "Equipamento hospitalar")]
    [InlineData(Categoria.Higiene, "Higiene")]
    [InlineData(Categoria.Microchip, "Microchip")]
    public void NomeDaCategoria_Devolve_O_Texto_Das_Seis_Categorias(Categoria categoria, string esperado)
    {
        Assert.Equal(esperado, Rotulos.NomeDaCategoria(categoria));
    }

    [Fact]
    public void NomeDaCategoria_Cobre_Todas_As_Categorias_Sem_Texto_Tecnico()
    {
        // Se alguém criar uma categoria nova e esquecer o texto, o nome em código (sem espaço) apareceria na tela.
        Assert.Equal(6, Enum.GetValues<Categoria>().Length);
        Assert.DoesNotContain("EquipamentoHospitalar",
            Enum.GetValues<Categoria>().Select(Rotulos.NomeDaCategoria));
    }

    [Theory]
    [InlineData(TipoOrigemEntrada.Compra, "Compra")]
    [InlineData(TipoOrigemEntrada.Concessao, "Concessão")]
    [InlineData(TipoOrigemEntrada.Doacao, "Doação")]
    public void NomeDaOrigem_Devolve_O_Texto_Com_Acento(TipoOrigemEntrada origem, string esperado)
    {
        Assert.Equal(esperado, Rotulos.NomeDaOrigem(origem));
    }

    [Theory]
    [InlineData(TipoMovimentacao.Entrada, "Entrada")]
    [InlineData(TipoMovimentacao.Saida, "Saída")]
    [InlineData(TipoMovimentacao.AjusteBalanco, "Ajuste de balanço")]
    public void NomeDoTipo_Devolve_O_Texto_Com_Acento(TipoMovimentacao tipo, string esperado)
    {
        Assert.Equal(esperado, Rotulos.NomeDoTipo(tipo));
    }

    [Theory]
    [InlineData(StatusMovimentacao.Ativa, "Ativa")]
    [InlineData(StatusMovimentacao.Corrigida, "Corrigida")]
    [InlineData(StatusMovimentacao.Cancelada, "Cancelada")]
    public void NomeDoStatus_Devolve_O_Texto_De_Cada_Status(StatusMovimentacao status, string esperado)
    {
        Assert.Equal(esperado, Rotulos.NomeDoStatus(status));
    }

    [Fact]
    public void Valor_De_Enum_Desconhecido_Cai_No_Nome_Do_Proprio_Valor_Sem_Quebrar()
    {
        // O caso "_" de cada switch: nunca lança exceção, mostra o que houver.
        Assert.Equal("99", Rotulos.NomeDaCategoria((Categoria)99));
        Assert.Equal("99", Rotulos.NomeDaOrigem((TipoOrigemEntrada)99));
        Assert.Equal("99", Rotulos.NomeDoTipo((TipoMovimentacao)99));
        Assert.Equal("99", Rotulos.NomeDoStatus((StatusMovimentacao)99));
    }

    // ---------- Texto do produto ----------

    private static Produto NovoProduto(string? apresentacao, string? especificacao) => new()
    {
        Nome = "Dipirona",
        Categoria = Categoria.Medicamento,
        Apresentacao = apresentacao,
        Especificacao = especificacao
    };

    [Fact]
    public void DetalheDoProduto_Com_Apresentacao_E_Especificacao_Junta_As_Tres_Partes()
    {
        var produto = NovoProduto("Ampola 2 ml", "500 mg");

        Assert.Equal("Medicamento · Ampola 2 ml · 500 mg", Rotulos.DetalheDoProduto(produto));
    }

    [Fact]
    public void DetalheDoProduto_So_Com_Apresentacao_Nao_Deixa_Separador_Sobrando()
    {
        var produto = NovoProduto("Frasco 10 ml", null);

        Assert.Equal("Medicamento · Frasco 10 ml", Rotulos.DetalheDoProduto(produto));
    }

    [Fact]
    public void DetalheDoProduto_So_Com_Especificacao_Nao_Deixa_Separador_Sobrando()
    {
        var produto = NovoProduto(null, "injetável");

        Assert.Equal("Medicamento · injetável", Rotulos.DetalheDoProduto(produto));
    }

    [Fact]
    public void DetalheDoProduto_Sem_Apresentacao_Nem_Especificacao_Mostra_So_A_Categoria()
    {
        var produto = NovoProduto(null, null);

        Assert.Equal("Medicamento", Rotulos.DetalheDoProduto(produto));
    }

    [Fact]
    public void DetalheDoProduto_Ignora_Partes_Vazias_Ou_So_Com_Espacos()
    {
        var produto = NovoProduto("", "   ");

        Assert.Equal("Medicamento", Rotulos.DetalheDoProduto(produto));
    }

    [Fact]
    public void DetalheDoProduto_Usa_O_Nome_Da_Categoria_Com_Espaco()
    {
        var produto = new Produto { Nome = "Mesa de inox", Categoria = Categoria.EquipamentoHospitalar };

        Assert.Equal("Equipamento hospitalar", Rotulos.DetalheDoProduto(produto));
    }

    [Fact]
    public void ProdutoNaLista_Com_Apresentacao_E_Especificacao_Comeca_Pelo_Nome_Sem_Categoria()
    {
        var produto = NovoProduto("Ampola 2 ml", "500 mg");

        Assert.Equal("Dipirona · Ampola 2 ml · 500 mg", Rotulos.ProdutoNaLista(produto));
    }

    [Fact]
    public void ProdutoNaLista_So_Com_Apresentacao()
    {
        var produto = NovoProduto("Ampola 2 ml", null);

        Assert.Equal("Dipirona · Ampola 2 ml", Rotulos.ProdutoNaLista(produto));
    }

    [Fact]
    public void ProdutoNaLista_So_Com_Especificacao()
    {
        var produto = NovoProduto(null, "500 mg");

        Assert.Equal("Dipirona · 500 mg", Rotulos.ProdutoNaLista(produto));
    }

    [Fact]
    public void ProdutoNaLista_Sem_Apresentacao_Nem_Especificacao_Mostra_So_O_Nome()
    {
        var produto = NovoProduto(null, null);

        Assert.Equal("Dipirona", Rotulos.ProdutoNaLista(produto));
    }

    [Fact]
    public void ProdutoNaLista_Ignora_Partes_Vazias_Ou_So_Com_Espacos()
    {
        var produto = NovoProduto("  ", "");

        Assert.Equal("Dipirona", Rotulos.ProdutoNaLista(produto));
    }

    // ---------- Validade ----------

    private static readonly DateOnly Hoje = new(2026, 10, 21);

    [Fact]
    public void Validade_Que_Vence_Hoje()
    {
        Assert.Equal("vence hoje (21/10/2026)", Rotulos.Validade(Hoje, Hoje));
    }

    [Fact]
    public void Validade_Que_Vence_Amanha()
    {
        Assert.Equal("vence amanhã (22/10/2026)", Rotulos.Validade(Hoje.AddDays(1), Hoje));
    }

    [Fact]
    public void Validade_Que_Vence_Em_Dois_Dias_Ja_Usa_O_Plural()
    {
        Assert.Equal("vence em 23/10/2026 (2 dias)", Rotulos.Validade(Hoje.AddDays(2), Hoje));
    }

    [Fact]
    public void Validade_Que_Vence_Em_Varios_Dias_Atravessando_O_Mes()
    {
        Assert.Equal("vence em 01/12/2026 (41 dias)", Rotulos.Validade(Hoje.AddDays(41), Hoje));
    }

    [Fact]
    public void Validade_Que_Vence_No_Ano_Seguinte()
    {
        Assert.Equal("vence em 12/03/2027 (142 dias)", Rotulos.Validade(new DateOnly(2027, 3, 12), Hoje));
    }

    [Fact]
    public void Validade_Que_Venceu_Ontem_Usa_Dia_No_Singular()
    {
        Assert.Equal("venceu em 20/10/2026 (há 1 dia)", Rotulos.Validade(Hoje.AddDays(-1), Hoje));
    }

    [Fact]
    public void Validade_Que_Venceu_Ha_Dois_Dias_Usa_Dias_No_Plural()
    {
        Assert.Equal("venceu em 19/10/2026 (há 2 dias)", Rotulos.Validade(Hoje.AddDays(-2), Hoje));
    }

    [Fact]
    public void Validade_Que_Venceu_Ha_Varios_Dias()
    {
        Assert.Equal("venceu em 13/09/2026 (há 38 dias)", Rotulos.Validade(Hoje.AddDays(-38), Hoje));
    }

    // ---------- Sinal ----------

    [Fact]
    public void ComSinal_Positivo_Leva_Sinal_De_Mais()
    {
        Assert.Equal("+5", Rotulos.ComSinal(5));
    }

    [Fact]
    public void ComSinal_Negativo_Leva_O_Sinal_De_Menos_Tipografico()
    {
        var texto = Rotulos.ComSinal(-3);

        Assert.Equal("\u22123", texto);
        Assert.DoesNotContain("-", texto); // não é o hífen do teclado
    }

    [Fact]
    public void ComSinal_Zero_Fica_Sem_Sinal()
    {
        Assert.Equal("0", Rotulos.ComSinal(0));
    }

    // ---------- Busca ----------

    [Theory]
    [InlineData("Dipirona", "dipi")]
    [InlineData("Dipirona", "DIPIRONA")]
    [InlineData("DIPIRONA SÓDICA", "sodica")]
    [InlineData("Injetável", "injetavel")]
    [InlineData("Injetavel", "INJETÁVEL")]
    [InlineData("Doação", "doacao")]
    [InlineData("Concessão", "CONCESSAO")]
    [InlineData("Ração para filhotes", "racao")]
    public void Contem_Ignora_Maiusculas_E_Acentos(string texto, string busca)
    {
        Assert.True(Rotulos.Contem(texto, busca));
    }

    [Theory]
    [InlineData("Dipirona", "ivermectina")]
    [InlineData("Dipirona", "dipironax")]
    [InlineData("", "dipirona")]
    public void Contem_Devolve_Falso_Quando_Nao_Encontra(string texto, string busca)
    {
        Assert.False(Rotulos.Contem(texto, busca));
    }

    [Fact]
    public void Contem_Com_Texto_Nulo_Devolve_Falso_Sem_Quebrar()
    {
        Assert.False(Rotulos.Contem(null, "dipirona"));
    }

    [Fact]
    public void Contem_Com_Busca_Vazia_Acha_Qualquer_Texto()
    {
        // Busca em branco = "mostrar tudo" nas telas.
        Assert.True(Rotulos.Contem("Dipirona", ""));
    }

    // ---------- Número digitado ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LerInteiro_Vazio_Devolve_Nulo(string? texto)
    {
        Assert.Null(Rotulos.LerInteiro(texto));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12abc")]
    [InlineData("1 2")]
    [InlineData("3,5")]
    [InlineData("1.5")]
    [InlineData("99999999999")] // maior que um inteiro: não estoura, vira "inválido"
    public void LerInteiro_Texto_Ou_Numero_Invalido_Devolve_Nulo(string texto)
    {
        Assert.Null(Rotulos.LerInteiro(texto));
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData("0", 0)]
    [InlineData("  7  ", 7)] // espaços nas pontas são ignorados
    [InlineData("-5", -5)] // negativo é lido; quem recusa é a validação da tela/Service
    public void LerInteiro_Numero_Valido_Devolve_O_Valor(string texto, int esperado)
    {
        Assert.Equal(esperado, Rotulos.LerInteiro(texto));
    }
}