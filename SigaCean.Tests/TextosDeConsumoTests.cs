using SigaCean.Models;
using SigaCean.Views;

namespace SigaCean.Tests;

// Textos dos gráficos e do Relatório por Categoria (Views/TextosDeConsumo.cs): são definidos num lugar só para a tela,
// os cartões e o PDF dizerem a mesma coisa. Os números vêm do RelatorioService; aqui só se confere a escrita.
public class TextosDeConsumoTests
{
    private readonly Cenario _c = new();

    private static readonly DateTime Inicio = new(2026, 10, 1);
    private static readonly DateTime Fim = new(2026, 10, 31);
    private static readonly DateTime DentroDoPeriodo = new(2026, 10, 15, 10, 0, 0);

    private ConsumoPorCategoria Linha(Categoria categoria) =>
        _c.Relatorios.GerarConsumoPorCategoria(Inicio, Fim).Single(l => l.Categoria == categoria);

    [Fact]
    public void Descrevem_Mais_Menos_Usado_E_Sem_Saida()
    {
        var dipirona = _c.NovoProdutoComEstoque(2000, nome: "Dipirona");
        var vacina = _c.NovoProdutoComEstoque(50, nome: "Vacina");
        _c.NovoProdutoComEstoque(5, nome: "Parado");
        _c.AdicionarMovimentacao(dipirona, TipoMovimentacao.Saida, 1200, DentroDoPeriodo);
        _c.AdicionarMovimentacao(vacina, TipoMovimentacao.Saida, 2, DentroDoPeriodo);
        var linha = Linha(Categoria.Medicamento);

        Assert.Equal("Mais usado: Dipirona (1.200 un.)", TextosDeConsumo.MaisUsado(linha));
        Assert.Equal("Menos usado: Vacina (2 un.)", TextosDeConsumo.MenosUsado(linha));
        Assert.Equal("1 produto(s) sem nenhuma saída no período.", TextosDeConsumo.SemSaida(linha));
        Assert.Contains("3 produto(s)", TextosDeConsumo.Resumo(linha));
    }

    [Fact]
    public void Categoria_Sem_Produto_Ou_Sem_Uso_Tem_Texto_Proprio()
    {
        var vazia = Linha(Categoria.Microchip);

        Assert.Equal("Nenhuma saída neste período.", TextosDeConsumo.MaisUsado(vazia));
        Assert.Equal(string.Empty, TextosDeConsumo.MenosUsado(vazia));
        Assert.Equal(string.Empty, TextosDeConsumo.SemSaida(vazia));
        Assert.Equal("Nenhum produto cadastrado nesta categoria.", TextosDeConsumo.Resumo(vazia));
    }

    [Theory]
    [InlineData(25, 100, "25%")]
    [InlineData(1, 3, "33%")]
    [InlineData(0, 50, "0%")]
    [InlineData(5, 0, "—")]
    [InlineData(1, 4621, "<1%")]
    public void Percentual_Arredonda_E_Protege_A_Divisao_Por_Zero(double valor, double total, string esperado)
    {
        Assert.Equal(esperado, TextosDeConsumo.Percentual(valor, total));
    }

    [Fact]
    public void Encurtar_Corta_Nomes_Longos_Com_Reticencias()
    {
        Assert.Equal("Dipirona", TextosDeConsumo.Encurtar("Dipirona", 20));
        Assert.Equal("Dipirona 500 mg…", TextosDeConsumo.Encurtar("Dipirona 500 mg injetável ampola", 16));
    }
}
