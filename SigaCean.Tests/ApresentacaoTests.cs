using System.Globalization;
using SigaCean.Models;
using SigaCean.Services;
using SigaCean.Views;

namespace SigaCean.Tests;

// Testa SÓ os textos de apresentação do estoque (Views/TextosDeEstoque.cs): validade, saldo, categoria + apresentação,
// mínimo e os botões de filtro. Aqui não há regra de estoque: as situações vêm prontas do EstoqueService e a tela
// só as repassa. As telas e o Linhas.cs usam MAUI e não compilam no projeto de testes.
//
// A cultura é fixada em "invariante" durante cada teste para o formato das datas (dd/MM/yyyy) não depender do idioma
// do Windows de quem roda.
public class ApresentacaoTests : IDisposable
{
    private readonly CultureInfo _culturaOriginal = CultureInfo.CurrentCulture;

    public ApresentacaoTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culturaOriginal;
    }

    private static readonly DateOnly Hoje = new(2026, 10, 2);

    // ---------- Validade e saldo ----------

    [Fact]
    public void DescreverValidade_Futura_Mostra_A_Data_E_Os_Dias()
    {
        // 02/10/2026 + 41 dias = 12/11/2026
        Assert.Equal("vence em 12/11/2026 (41 dias)", TextosDeEstoque.DescreverValidade(new DateOnly(2026, 11, 12), Hoje));
    }

    [Fact]
    public void DescreverValidade_Hoje_Amanha_E_Vencida()
    {
        Assert.Equal("vence hoje (02/10/2026)", TextosDeEstoque.DescreverValidade(Hoje, Hoje));
        Assert.Equal("vence amanhã (03/10/2026)", TextosDeEstoque.DescreverValidade(Hoje.AddDays(1), Hoje));
        Assert.Equal("venceu em 01/10/2026 (há 1 dia)", TextosDeEstoque.DescreverValidade(Hoje.AddDays(-1), Hoje));
        Assert.Equal("venceu em 24/09/2026 (há 8 dias)", TextosDeEstoque.DescreverValidade(Hoje.AddDays(-8), Hoje));
    }

    [Fact]
    public void DescreverSaldo_Com_Data_Descreve_A_Validade()
    {
        Assert.Equal("vence em 12/11/2026 (41 dias)", TextosDeEstoque.DescreverSaldo(new DateOnly(2026, 11, 12), Hoje));
    }

    [Fact]
    public void DescreverSaldo_Sem_Data_Diz_Que_Sai_Por_Ultimo()
    {
        Assert.Equal("Sai por último", TextosDeEstoque.DescreverSaldo(null, Hoje));
    }

    [Theory]
    [InlineData(1, "1 un.")]
    [InlineData(25, "25 un.")]
    public void QuantidadeDoSaldo_Escreve_Unidades(int quantidade, string esperado)
    {
        Assert.Equal(esperado, TextosDeEstoque.QuantidadeDoSaldo(quantidade));
    }

    [Theory]
    [InlineData(1, "1º")]
    [InlineData(3, "3º")]
    public void OrdemDoSaldo_Escreve_A_Posicao_Na_Fila(int posicao, string esperado)
    {
        Assert.Equal(esperado, TextosDeEstoque.OrdemDoSaldo(posicao));
    }

    // ---------- Resumo dos saldos (linha da lista do Estoque) ----------

    [Fact]
    public void ResumoDosSaldos_Com_Validade_E_Sem_Validade_Junta_Na_Ordem_Recebida()
    {
        var saldos = new List<SaldoValidade>
        {
            new() { DataValidade = new DateOnly(2026, 11, 12), Quantidade = 4 },
            new() { DataValidade = null, Quantidade = 3 }
        };

        Assert.Equal("12/11/2026: 4 un. · sem validade: 3 un.", TextosDeEstoque.ResumoDosSaldos(saldos));
    }

    [Fact]
    public void ResumoDosSaldos_Um_Saldo_Sem_Validade_Nao_Mostra_Nada()
    {
        var saldos = new List<SaldoValidade> { new() { DataValidade = null, Quantidade = 10 } };

        Assert.Equal(string.Empty, TextosDeEstoque.ResumoDosSaldos(saldos));
    }

    [Fact]
    public void ResumoDosSaldos_Um_Saldo_Com_Validade_Mostra()
    {
        var saldos = new List<SaldoValidade> { new() { DataValidade = new DateOnly(2027, 1, 5), Quantidade = 2 } };

        Assert.Equal("05/01/2027: 2 un.", TextosDeEstoque.ResumoDosSaldos(saldos));
    }

    [Fact]
    public void ResumoDosSaldos_Ignora_Saldo_Zerado_E_Lista_Vazia()
    {
        var saldos = new List<SaldoValidade>
        {
            new() { DataValidade = new DateOnly(2026, 11, 12), Quantidade = 0 },
            new() { DataValidade = null, Quantidade = 6 }
        };

        Assert.Equal(string.Empty, TextosDeEstoque.ResumoDosSaldos(saldos)); // sobrou um saldo só, sem validade
        Assert.Equal(string.Empty, TextosDeEstoque.ResumoDosSaldos(new List<SaldoValidade>()));
    }

    // ---------- Categoria, apresentação e mínimo ----------

    [Fact]
    public void CategoriaComApresentacao_Junta_Categoria_Apresentacao_E_Especificacao()
    {
        var produto = new Produto { Nome = "Dipirona", Categoria = Categoria.Medicamento, Apresentacao = "Ampola 2 ml", Especificacao = "500 mg" };

        Assert.Equal("Medicamento · Ampola 2 ml · 500 mg", TextosDeEstoque.CategoriaComApresentacao(produto));
    }

    [Fact]
    public void CategoriaComApresentacao_Sem_Apresentacao_Mostra_So_A_Categoria()
    {
        var produto = new Produto { Nome = "Microchip", Categoria = Categoria.Microchip };

        Assert.Equal("Microchip", TextosDeEstoque.CategoriaComApresentacao(produto));
    }

    [Fact]
    public void Minimo_Escreve_O_Valor()
    {
        Assert.Equal("mínimo: 10", TextosDeEstoque.Minimo(10));
    }

    [Fact]
    public void MinimoNaFicha_Mostra_O_Valor_Ou_Avisa_Que_Nao_Foi_Definido()
    {
        Assert.Equal("Estoque mínimo: 10", TextosDeEstoque.MinimoNaFicha(10));
        Assert.Equal("Estoque mínimo: 0", TextosDeEstoque.MinimoNaFicha(0));
        Assert.Equal("Estoque mínimo não definido.", TextosDeEstoque.MinimoNaFicha(null));
    }

    // ---------- Quadros do Painel e botões do Estoque ----------

    [Theory]
    [InlineData(0, "nenhum produto")]
    [InlineData(1, "1 produto")]
    [InlineData(42, "42 produtos")]
    public void ContagemDeProdutos_Concorda_No_Singular_E_No_Plural(int quantidade, string esperado)
    {
        Assert.Equal(esperado, TextosDeEstoque.ContagemDeProdutos(quantidade));
    }

    [Theory]
    [InlineData(FiltroDeSituacao.Todos, "Todos")]
    [InlineData(FiltroDeSituacao.SemEstoque, "Sem estoque")]
    [InlineData(FiltroDeSituacao.AbaixoDoMinimo, "Abaixo do mínimo")]
    [InlineData(FiltroDeSituacao.Vencidos, "Vencidos")]
    [InlineData(FiltroDeSituacao.ValidadeProxima, "Validade próxima")]
    public void NomeDoFiltro_Devolve_O_Texto_Do_Botao(FiltroDeSituacao filtro, string esperado)
    {
        Assert.Equal(esperado, TextosDeEstoque.NomeDoFiltro(filtro));
    }

    [Fact]
    public void NomeDoFiltro_Cobre_Todos_Os_Filtros_Sem_Texto_Tecnico()
    {
        // Se alguém criar um filtro novo e esquecer o texto, o nome em código apareceria no botão.
        Assert.Equal(5, Enum.GetValues<FiltroDeSituacao>().Length);
        Assert.DoesNotContain("ValidadeProxima", Enum.GetValues<FiltroDeSituacao>().Select(TextosDeEstoque.NomeDoFiltro));
        Assert.DoesNotContain("SemEstoque", Enum.GetValues<FiltroDeSituacao>().Select(TextosDeEstoque.NomeDoFiltro));
    }

    // ---------- Qual filtro olha qual situação ----------

    [Fact]
    public void Atende_Todos_Aceita_Qualquer_Produto()
    {
        Assert.True(TextosDeEstoque.Atende(FiltroDeSituacao.Todos, false, false, false, false));
        Assert.True(TextosDeEstoque.Atende(FiltroDeSituacao.Todos, true, true, true, true));
    }

    [Fact]
    public void Atende_Cada_Filtro_Olha_So_A_Sua_Situacao()
    {
        // Cada situação ligada sozinha: só o filtro correspondente aceita.
        Assert.True(TextosDeEstoque.Atende(FiltroDeSituacao.SemEstoque, true, false, false, false));
        Assert.False(TextosDeEstoque.Atende(FiltroDeSituacao.SemEstoque, false, true, true, true));

        Assert.True(TextosDeEstoque.Atende(FiltroDeSituacao.AbaixoDoMinimo, false, true, false, false));
        Assert.False(TextosDeEstoque.Atende(FiltroDeSituacao.AbaixoDoMinimo, true, false, true, true));

        Assert.True(TextosDeEstoque.Atende(FiltroDeSituacao.Vencidos, false, false, true, false));
        Assert.False(TextosDeEstoque.Atende(FiltroDeSituacao.Vencidos, true, true, false, true));

        Assert.True(TextosDeEstoque.Atende(FiltroDeSituacao.ValidadeProxima, false, false, false, true));
        Assert.False(TextosDeEstoque.Atende(FiltroDeSituacao.ValidadeProxima, true, true, true, false));
    }

    // ---------- A fila FEFO que a ficha mostra ----------

    [Fact]
    public void Fila_Da_Ficha_Vem_Do_EstoqueService_Na_Ordem_Fefo_E_Nao_Altera_O_Estoque()
    {
        // A ficha (ProdutoLinha.SaldosEmOrdemFefo) pede ao EstoqueService a previsão de retirar o estoque INTEIRO:
        // essa previsão é exatamente a fila em que os saldos saem. Aqui se confere esse caminho.
        var cenario = new Cenario();
        var produto = cenario.NovoProduto();
        cenario.Estoque.AumentarEstoque(produto, 5, new DateOnly(2026, 12, 1));
        cenario.Estoque.AumentarEstoque(produto, 3, null);
        cenario.Estoque.AumentarEstoque(produto, 2, new DateOnly(2026, 11, 1));

        var fila = cenario.Estoque.PrevisaoFefo(produto, produto.Quantidade);

        Assert.Equal(new DateOnly?[] { new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 1), null }, fila.Select(f => f.DataValidade).ToArray());
        Assert.Equal(new[] { 2, 5, 3 }, fila.Select(f => f.Quantidade).ToArray());
        Assert.Equal(10, produto.Quantidade); // só previsão: nada foi retirado
    }
}
