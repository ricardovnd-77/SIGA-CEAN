using SigaCean.Models;

namespace SigaCean.Tests;

public class ConsumoPorCategoriaTests
{
    private readonly Cenario _c = new();

    private static readonly DateTime Inicio = new(2026, 10, 1);
    private static readonly DateTime Fim = new(2026, 10, 31);
    private static readonly DateTime DentroDoPeriodo = new(2026, 10, 15, 10, 0, 0);

    private ConsumoPorCategoria Linha(Categoria categoria) =>
        _c.Relatorios.GerarConsumoPorCategoria(Inicio, Fim).Single(l => l.Categoria == categoria);

    private void Saiu(Produto produto, int quantidade, DateTime? data = null, StatusMovimentacao status = StatusMovimentacao.Ativa) =>
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, quantidade, data ?? DentroDoPeriodo, status);

    // ---------- Uma linha por categoria ----------

    [Fact]
    public void Devolve_Uma_Linha_Para_Cada_Categoria_Mesmo_Sem_Nenhum_Produto()
    {
        var linhas = _c.Relatorios.GerarConsumoPorCategoria(Inicio, Fim);

        Assert.Equal(Enum.GetValues<Categoria>(), linhas.Select(l => l.Categoria));
        Assert.All(linhas, l =>
        {
            Assert.Equal(0, l.QuantidadeDeProdutos);
            Assert.Equal(0, l.EstoqueAtual);
            Assert.Equal(0, l.TotalSaidas);
            Assert.Equal(0, l.ProdutosSemSaida);
            Assert.Null(l.MaisUsado);
            Assert.Null(l.MenosUsado);
            Assert.Empty(l.ProdutosUsados);
        });
    }

    [Fact]
    public void Cada_Produto_Conta_So_Na_Sua_Categoria()
    {
        var remedio = _c.NovoProdutoComEstoque(10, nome: "Dipirona", categoria: Categoria.Medicamento);
        var racao = _c.NovoProdutoComEstoque(7, nome: "Ração", categoria: Categoria.Alimentar);
        Saiu(remedio, 4);
        Saiu(racao, 2);

        var medicamento = Linha(Categoria.Medicamento);
        var alimentar = Linha(Categoria.Alimentar);

        Assert.Equal(1, medicamento.QuantidadeDeProdutos);
        Assert.Equal(10, medicamento.EstoqueAtual);
        Assert.Equal(4, medicamento.TotalSaidas);
        Assert.Equal(1, alimentar.QuantidadeDeProdutos);
        Assert.Equal(7, alimentar.EstoqueAtual);
        Assert.Equal(2, alimentar.TotalSaidas);
        Assert.Equal(0, Linha(Categoria.Higiene).TotalSaidas);
    }

    // ---------- O que conta como uso ----------

    [Fact]
    public void So_Saidas_Ativas_Do_Periodo_Contam_Como_Uso()
    {
        var produto = _c.NovoProduto();
        Saiu(produto, 5);
        Saiu(produto, 100, status: StatusMovimentacao.Cancelada);
        Saiu(produto, 200, status: StatusMovimentacao.Corrigida);
        Saiu(produto, 9, new DateTime(2026, 9, 30, 23, 59, 59)); // antes do período
        Saiu(produto, 8, new DateTime(2026, 11, 1)); // depois do período
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 300, DentroDoPeriodo);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.AjusteBalanco, -50, DentroDoPeriodo);

        var linha = Linha(Categoria.Medicamento);

        Assert.Equal(5, linha.TotalSaidas);
        var usado = Assert.Single(linha.ProdutosUsados);
        Assert.Same(produto, usado.Produto);
        Assert.Equal(5, usado.TotalSaidas);
    }

    [Fact]
    public void Produto_So_Com_Entrada_Ajuste_Ou_Saida_Cancelada_Fica_Sem_Saida()
    {
        var produto = _c.NovoProduto();
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 30, DentroDoPeriodo);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.AjusteBalanco, -2, DentroDoPeriodo);
        Saiu(produto, 6, status: StatusMovimentacao.Cancelada);

        var linha = Linha(Categoria.Medicamento);

        Assert.Equal(0, linha.TotalSaidas);
        Assert.Equal(1, linha.ProdutosSemSaida);
        Assert.Empty(linha.ProdutosUsados);
        Assert.Null(linha.MaisUsado);
    }

    [Fact]
    public void O_Dia_Final_Do_Periodo_Entra_Inteiro()
    {
        var produto = _c.NovoProduto();
        Saiu(produto, 3, new DateTime(2026, 10, 31, 23, 59, 59));

        Assert.Equal(3, Linha(Categoria.Medicamento).TotalSaidas);
    }

    [Fact]
    public void Produto_Inativo_Fica_De_Fora_De_Tudo()
    {
        var ativo = _c.NovoProdutoComEstoque(5, nome: "Ativo");
        var inativo = _c.NovoProdutoComEstoque(50, nome: "Inativo");
        Saiu(ativo, 2);
        Saiu(inativo, 40);
        inativo.Ativo = false;

        var linha = Linha(Categoria.Medicamento);

        Assert.Equal(1, linha.QuantidadeDeProdutos);
        Assert.Equal(5, linha.EstoqueAtual);
        Assert.Equal(2, linha.TotalSaidas);
        Assert.Equal(0, linha.ProdutosSemSaida);
        Assert.DoesNotContain(linha.ProdutosUsados, c => c.Produto == inativo);
    }

    // ---------- Mais e menos usado ----------

    [Fact]
    public void Ordena_Do_Mais_Ao_Menos_Usado_E_Desempata_Por_Nome()
    {
        var pouco = _c.NovoProduto("Aaa pouco");
        var muito = _c.NovoProduto("Zzz muito");
        var empateB = _c.NovoProduto("Bbb empate");
        var empateA = _c.NovoProduto("Abb empate");
        Saiu(pouco, 1);
        Saiu(muito, 20);
        Saiu(empateB, 7);
        Saiu(empateA, 7);

        var linha = Linha(Categoria.Medicamento);

        Assert.Equal(
            new[] { "Zzz muito", "Abb empate", "Bbb empate", "Aaa pouco" },
            linha.ProdutosUsados.Select(c => c.Produto.Nome));
        Assert.Equal(35, linha.TotalSaidas);
        var maisUsado = linha.MaisUsado!;
        var menosUsado = linha.MenosUsado!;
        Assert.Same(muito, maisUsado.Produto);
        Assert.Equal(20, maisUsado.TotalSaidas);
        Assert.Same(pouco, menosUsado.Produto);
        Assert.Equal(1, menosUsado.TotalSaidas);
    }

    [Fact]
    public void Soma_As_Varias_Saidas_Do_Mesmo_Produto()
    {
        var produto = _c.NovoProduto();
        Saiu(produto, 3);
        Saiu(produto, 4, new DateTime(2026, 10, 20));

        var usado = Assert.Single(Linha(Categoria.Medicamento).ProdutosUsados);

        Assert.Equal(7, usado.TotalSaidas);
    }

    [Fact]
    public void Menos_Usado_E_Nulo_Quando_So_Um_Produto_Teve_Saida()
    {
        var usado = _c.NovoProduto("Usado");
        _c.NovoProduto("Parado");
        Saiu(usado, 5);

        var linha = Linha(Categoria.Medicamento);

        Assert.Same(usado, linha.MaisUsado!.Produto);
        Assert.Null(linha.MenosUsado);
        Assert.Equal(1, linha.ProdutosSemSaida);
    }

    [Fact]
    public void Produto_Sem_Saida_Conta_Em_ProdutosSemSaida_E_Nunca_E_O_Menos_Usado()
    {
        var a = _c.NovoProduto("Aaa");
        var b = _c.NovoProduto("Bbb");
        _c.NovoProduto("Parado 1");
        _c.NovoProduto("Parado 2");
        Saiu(a, 10);
        Saiu(b, 2);

        var linha = Linha(Categoria.Medicamento);

        Assert.Equal(4, linha.QuantidadeDeProdutos);
        Assert.Equal(2, linha.ProdutosSemSaida);
        Assert.Same(b, linha.MenosUsado!.Produto);
        Assert.All(linha.ProdutosUsados, c => Assert.True(c.TotalSaidas > 0));
    }

    // ---------- Coerência com o Relatório de Consumo e períodos ----------

    [Fact]
    public void Total_De_Saidas_Por_Categoria_Bate_Com_O_Relatorio_De_Consumo()
    {
        var remedio1 = _c.NovoProduto("Remédio 1", Categoria.Medicamento);
        var remedio2 = _c.NovoProduto("Remédio 2", Categoria.Medicamento);
        var racao = _c.NovoProduto("Ração", Categoria.Alimentar);
        var seringa = _c.NovoProduto("Seringa", Categoria.Insumo);
        Saiu(remedio1, 12);
        Saiu(remedio2, 3);
        Saiu(racao, 40);
        Saiu(seringa, 6);
        Saiu(seringa, 99, status: StatusMovimentacao.Cancelada);
        _c.AdicionarMovimentacao(racao, TipoMovimentacao.Entrada, 500, DentroDoPeriodo);

        var porCategoria = _c.Relatorios.GerarConsumoPorCategoria(Inicio, Fim);
        var relatorio = _c.Relatorios.GerarRelatorioConsumo(Inicio, Fim);

        Assert.Equal(relatorio.Sum(i => i.TotalSaidas), porCategoria.Sum(l => l.TotalSaidas));
        foreach (var categoria in Enum.GetValues<Categoria>())
        {
            var doRelatorio = _c.Relatorios.GerarRelatorioConsumo(Inicio, Fim, categoria).Sum(i => i.TotalSaidas);
            Assert.Equal(doRelatorio, porCategoria.Single(l => l.Categoria == categoria).TotalSaidas);
        }
    }

    [Fact]
    public void Datas_Invertidas_Sao_Recusadas()
    {
        Assert.Throws<InvalidOperationException>(() => _c.Relatorios.GerarConsumoPorCategoria(Fim, Inicio));
    }

    [Fact]
    public void Nao_Altera_O_Estoque_Nem_O_Historico()
    {
        var produto = _c.NovoProdutoComEstoque(10, Cenario.Hoje.AddDays(90));
        Saiu(produto, 3);
        var antes = Cenario.Foto(produto);
        var movimentacoes = _c.Dados.Movimentacoes.Count;

        _c.Relatorios.GerarConsumoPorCategoria(Inicio, Fim);

        Assert.Equal(antes, Cenario.Foto(produto));
        Assert.Equal(movimentacoes, _c.Dados.Movimentacoes.Count);
    }
}