using SigaCean.Models;
using SigaCean.Views;

namespace SigaCean.Tests;

// "Estoque crítico": quantidade <= metade do mínimo (é um caso mais grave de "abaixo do mínimo"). A conta é só do
// EstoqueService; a tela apenas mostra o selo e o filtro.
public class EstoqueCriticoTests
{
    private readonly Cenario _c = new();

    [Theory]
    [InlineData(10, 5, true, true)]   // exatamente a metade do mínimo
    [InlineData(10, 4, true, true)]
    [InlineData(10, 6, false, true)]  // baixo, mas ainda não crítico
    [InlineData(10, 10, false, true)] // exatamente no mínimo: baixo
    [InlineData(10, 11, false, false)]
    public void Critico_E_Abaixo_Do_Minimo_Seguem_As_Regras(int minimo, int quantidade, bool critico, bool abaixo)
    {
        var produto = _c.NovoProdutoComEstoque(quantidade, minimo: minimo);

        Assert.Equal(critico, _c.Estoque.EstaCritico(produto));
        Assert.Equal(abaixo, _c.Estoque.EstaAbaixoDoMinimo(produto));
    }

    [Fact]
    public void Sem_Minimo_Cadastrado_Nunca_E_Critico()
    {
        var produto = _c.NovoProdutoComEstoque(1);

        Assert.False(_c.Estoque.EstaCritico(produto));
    }

    [Fact]
    public void Filtro_Critico_Usa_So_A_Situacao_Critica()
    {
        Assert.True(TextosDeEstoque.Atende(FiltroDeSituacao.Critico, false, true, false, false, critico: true));
        Assert.False(TextosDeEstoque.Atende(FiltroDeSituacao.Critico, false, true, false, false, critico: false));
        Assert.Equal("Estoque crítico", TextosDeEstoque.NomeDoFiltro(FiltroDeSituacao.Critico));
    }
}
