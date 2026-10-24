using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Tests;

public class RelatorioServiceTests
{
    private readonly Cenario _c = new();

    private static readonly DateTime Inicio = new(2026, 10, 1);
    private static readonly DateTime Fim = new(2026, 10, 31);
    private static readonly DateTime DentroDoPeriodo = new(2026, 10, 15, 10, 0, 0);

    // ---------- Relatório de consumo ----------

    [Fact]
    public void Consumo_Soma_Entradas_Saidas_E_Ajustes_Por_Produto_E_Mostra_O_Estoque_Atual()
    {
        var produto = _c.NovoProdutoComEstoque(50);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 30, DentroDoPeriodo);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 20, DentroDoPeriodo);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 7, DentroDoPeriodo);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 3, DentroDoPeriodo);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.AjusteBalanco, 5, DentroDoPeriodo);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.AjusteBalanco, -2, DentroDoPeriodo);

        var item = Assert.Single(_c.Relatorios.GerarRelatorioConsumo(Inicio, Fim));

        Assert.Same(produto, item.Produto);
        Assert.Equal(50, item.TotalEntradas);
        Assert.Equal(10, item.TotalSaidas);
        Assert.Equal(3, item.TotalAjustes); // soma com sinal: +5 e -2
        Assert.Equal(50, item.EstoqueAtual);
    }

    [Fact]
    public void Consumo_Ignora_Movimentacoes_Canceladas_E_Corrigidas()
    {
        var produto = _c.NovoProduto();
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 5, DentroDoPeriodo);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 100, DentroDoPeriodo, StatusMovimentacao.Cancelada);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 200, DentroDoPeriodo, StatusMovimentacao.Corrigida);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 300, DentroDoPeriodo, StatusMovimentacao.Cancelada);

        var item = Assert.Single(_c.Relatorios.GerarRelatorioConsumo(Inicio, Fim));

        Assert.Equal(5, item.TotalSaidas);
        Assert.Equal(0, item.TotalEntradas);
    }

    [Fact]
    public void Produto_So_Com_Movimentacao_Cancelada_Nao_Aparece_No_Consumo()
    {
        var produto = _c.NovoProduto();
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 5, DentroDoPeriodo, StatusMovimentacao.Cancelada);

        Assert.Empty(_c.Relatorios.GerarRelatorioConsumo(Inicio, Fim));
    }

    [Fact]
    public void Movimentacao_Fora_Do_Periodo_Nao_Aparece()
    {
        var produto = _c.NovoProduto();
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 5, new DateTime(2026, 9, 30, 23, 59, 59));
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 8, new DateTime(2026, 11, 1, 0, 0, 0));
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 2, DentroDoPeriodo);

        var item = Assert.Single(_c.Relatorios.GerarRelatorioConsumo(Inicio, Fim));

        Assert.Equal(2, item.TotalSaidas);
    }

    [Fact]
    public void Consumo_Inclui_O_Dia_Final_Inteiro()
    {
        var produto = _c.NovoProduto();
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 4, new DateTime(2026, 10, 31, 23, 30, 0));

        var item = Assert.Single(_c.Relatorios.GerarRelatorioConsumo(Inicio, new DateTime(2026, 10, 31)));

        Assert.Equal(4, item.TotalSaidas);
    }

    [Fact]
    public void Consumo_Filtra_Por_Categoria()
    {
        var remedio = _c.NovoProduto("Dipirona", Categoria.Medicamento);
        var racao = _c.NovoProduto("Ração", Categoria.Alimentar);
        _c.AdicionarMovimentacao(remedio, TipoMovimentacao.Saida, 5, DentroDoPeriodo);
        _c.AdicionarMovimentacao(racao, TipoMovimentacao.Saida, 9, DentroDoPeriodo);

        var soAlimentar = _c.Relatorios.GerarRelatorioConsumo(Inicio, Fim, Categoria.Alimentar);
        var todas = _c.Relatorios.GerarRelatorioConsumo(Inicio, Fim);

        Assert.Same(racao, Assert.Single(soAlimentar).Produto);
        Assert.Equal(2, todas.Count);
    }

    [Fact]
    public void Consumo_Ordena_Por_Maior_Saida_E_Desempata_Por_Nome()
    {
        var pouco = _c.NovoProduto("Aaa pouco");
        var muito = _c.NovoProduto("Zzz muito");
        var empateB = _c.NovoProduto("Bbb empate");
        var empateA = _c.NovoProduto("Abb empate");
        _c.AdicionarMovimentacao(pouco, TipoMovimentacao.Saida, 1, DentroDoPeriodo);
        _c.AdicionarMovimentacao(muito, TipoMovimentacao.Saida, 50, DentroDoPeriodo);
        _c.AdicionarMovimentacao(empateB, TipoMovimentacao.Saida, 10, DentroDoPeriodo);
        _c.AdicionarMovimentacao(empateA, TipoMovimentacao.Saida, 10, DentroDoPeriodo);

        var resultado = _c.Relatorios.GerarRelatorioConsumo(Inicio, Fim);

        Assert.Equal(new[] { muito, empateA, empateB, pouco }, resultado.Select(i => i.Produto));
    }

    [Fact]
    public void Periodo_Com_Inicio_Depois_Do_Fim_E_Recusado_Em_Todos_Os_Relatorios()
    {
        var inicio = new DateTime(2026, 10, 20);
        var fim = new DateTime(2026, 10, 1);

        Assert.Throws<InvalidOperationException>(() => _c.Relatorios.GerarRelatorioConsumo(inicio, fim));
        Assert.Throws<InvalidOperationException>(() => _c.Relatorios.ObterEntradas(inicio, fim));
        Assert.Throws<InvalidOperationException>(() => _c.Relatorios.ObterSaidas(inicio, fim));
    }

    // ---------- Entradas e saídas ----------

    [Fact]
    public void Entradas_Do_Periodo_So_Ativas_Do_Tipo_Certo_Da_Mais_Recente_Para_A_Mais_Antiga()
    {
        var produto = _c.NovoProduto();
        var antiga = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 2));
        var recente = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 20));
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 10), StatusMovimentacao.Cancelada);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 11), StatusMovimentacao.Corrigida);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 1, new DateTime(2026, 10, 12));
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 12, 1)); // fora do período

        var resultado = _c.Relatorios.ObterEntradas(Inicio, Fim);

        Assert.Equal(new[] { recente, antiga }, resultado);
    }

    [Fact]
    public void Saidas_Do_Periodo_So_Ativas_Do_Tipo_Certo_Da_Mais_Recente_Para_A_Mais_Antiga()
    {
        var produto = _c.NovoProduto();
        var antiga = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 1, new DateTime(2026, 10, 2));
        var recente = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 1, new DateTime(2026, 10, 31, 22, 0, 0));
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 1, new DateTime(2026, 10, 10), StatusMovimentacao.Cancelada);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 12));
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.AjusteBalanco, 1, new DateTime(2026, 10, 13));

        var resultado = _c.Relatorios.ObterSaidas(Inicio, Fim);

        Assert.Equal(new[] { recente, antiga }, resultado);
    }

    // ---------- Estoque disponível, baixo e validade ----------

    [Fact]
    public void Estoque_Disponivel_So_Ativos_Com_Quantidade_Maior_Que_Zero_Por_Nome()
    {
        var b = _c.NovoProdutoComEstoque(3, nome: "Bbb");
        var a = _c.NovoProdutoComEstoque(1, nome: "Aaa");
        _c.NovoProduto("Zerado");
        var inativo = _c.NovoProdutoComEstoque(9, nome: "Inativo");
        inativo.Ativo = false;

        var resultado = _c.Relatorios.ObterEstoqueDisponivel();

        Assert.Equal(new[] { a, b }, resultado);
    }

    [Fact]
    public void Estoque_Baixo_Usa_A_Regra_Do_EstoqueService()
    {
        var noMinimo = _c.NovoProdutoComEstoque(5, nome: "No minimo", minimo: 5);
        var abaixo = _c.NovoProdutoComEstoque(2, nome: "Abaixo", minimo: 5);
        var zerado = _c.NovoProduto("Zerado", minimo: 5);
        _c.NovoProdutoComEstoque(6, nome: "Acima", minimo: 5);
        _c.NovoProdutoComEstoque(1, nome: "Sem minimo", minimo: null);
        var inativo = _c.NovoProdutoComEstoque(1, nome: "Inativo", minimo: 5);
        inativo.Ativo = false;

        var resultado = _c.Relatorios.ObterProdutosComEstoqueBaixo();

        Assert.Equal(new[] { abaixo, noMinimo, zerado }, resultado);
        Assert.All(resultado, p => Assert.True(_c.Estoque.EstaAbaixoDoMinimo(p)));
    }

    [Fact]
    public void Validade_Proxima_Usa_A_Regra_Do_EstoqueService()
    {
        var hoje = Cenario.Hoje;
        var vence10Dias = _c.NovoProdutoComEstoque(5, hoje.AddDays(10), "Vence em 10 dias");
        var venceNoLimite = _c.NovoProdutoComEstoque(5, hoje.AddDays(EstoqueService.DiasAlertaValidade), "Vence no limite");
        _c.NovoProdutoComEstoque(5, hoje.AddDays(EstoqueService.DiasAlertaValidade + 1), "Vence longe");
        _c.NovoProdutoComEstoque(5, hoje.AddDays(-3), "Vencido");   // vencido não é "próximo"
        _c.NovoProdutoComEstoque(5, null, "Sem validade");
        var inativo = _c.NovoProdutoComEstoque(5, hoje.AddDays(5), "Inativo");
        inativo.Ativo = false;

        var resultado = _c.Relatorios.ObterProdutosProximosDaValidade();

        Assert.Equal(new[] { vence10Dias, venceNoLimite }, resultado);
        Assert.All(resultado, p => Assert.True(_c.Estoque.TemValidadeProxima(p, hoje)));
    }

    [Fact]
    public void Validade_Proxima_Ignora_Saldo_Ja_Zerado()
    {
        var hoje = Cenario.Hoje;
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 5, hoje.AddDays(3));
        _c.Estoque.DiminuirEstoque(produto, 5);

        Assert.Empty(_c.Relatorios.ObterProdutosProximosDaValidade());
    }

    // ---------- Média mensal, ordem e última movimentação ----------

    [Fact]
    public void Consumo_Calcula_A_Media_Mensal_De_Saidas_Com_Uma_Casa_Decimal()
    {
        var produto = _c.NovoProduto();
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 10, DentroDoPeriodo);

        var item = Assert.Single(_c.Relatorios.GerarRelatorioConsumo(Inicio, Fim));

        // outubro tem 31 dias = 31 / 30,4375 meses; 10 / 1,0185 = 9,82 -> 9,8
        Assert.Equal(9.8m, item.MediaMensalSaidas);
    }

    [Fact]
    public void Media_Mensal_Em_Periodo_Anual_Divide_Pelos_Doze_Meses()
    {
        var produto = _c.NovoProduto();
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 24, new DateTime(2026, 3, 10));

        var item = Assert.Single(_c.Relatorios.GerarRelatorioConsumo(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)));

        Assert.Equal(2.0m, item.MediaMensalSaidas);
    }

    [Fact]
    public void Media_Mensal_Em_Periodo_Menor_Que_Um_Mes_Conta_Como_Um_Mes()
    {
        var produto = _c.NovoProduto();
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 7, new DateTime(2026, 10, 5, 9, 0, 0));

        var item = Assert.Single(_c.Relatorios.GerarRelatorioConsumo(new DateTime(2026, 10, 5), new DateTime(2026, 10, 5)));

        Assert.Equal(7m, item.MediaMensalSaidas);
    }

    [Fact]
    public void Media_Mensal_So_Considera_Saidas_Ativas()
    {
        var produto = _c.NovoProduto();
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 5, DentroDoPeriodo);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 500, DentroDoPeriodo, StatusMovimentacao.Cancelada);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 900, DentroDoPeriodo);

        var item = Assert.Single(_c.Relatorios.GerarRelatorioConsumo(Inicio, Fim));

        Assert.Equal(4.9m, item.MediaMensalSaidas); // 5 / 1,0185 = 4,91
    }

    [Fact]
    public void Consumo_Sem_Informar_A_Ordem_Continua_Por_Maior_Consumo_E_Depois_Nome()
    {
        var a = _c.NovoProduto("Aaa");
        var b = _c.NovoProduto("Bbb");
        var c = _c.NovoProduto("Ccc");
        _c.AdicionarMovimentacao(a, TipoMovimentacao.Saida, 1, DentroDoPeriodo);
        _c.AdicionarMovimentacao(b, TipoMovimentacao.Saida, 9, DentroDoPeriodo);
        _c.AdicionarMovimentacao(c, TipoMovimentacao.Saida, 9, DentroDoPeriodo);

        var padrao = _c.Relatorios.GerarRelatorioConsumo(Inicio, Fim).Select(i => i.Produto.Nome);
        var explicita = _c.Relatorios.GerarRelatorioConsumo(Inicio, Fim, null, OrdemRelatorio.MaiorConsumo).Select(i => i.Produto.Nome);

        Assert.Equal(new[] { "Bbb", "Ccc", "Aaa" }, padrao);
        Assert.Equal(new[] { "Bbb", "Ccc", "Aaa" }, explicita);
    }

    [Fact]
    public void Consumo_Ordenado_Por_Nome_Ignora_O_Consumo()
    {
        var zeta = _c.NovoProduto("Zeta");
        var alfa = _c.NovoProduto("alfa");
        var mega = _c.NovoProduto("Mega");
        _c.AdicionarMovimentacao(zeta, TipoMovimentacao.Saida, 50, DentroDoPeriodo);
        _c.AdicionarMovimentacao(alfa, TipoMovimentacao.Saida, 1, DentroDoPeriodo);
        _c.AdicionarMovimentacao(mega, TipoMovimentacao.Saida, 20, DentroDoPeriodo);

        var nomes = _c.Relatorios.GerarRelatorioConsumo(Inicio, Fim, null, OrdemRelatorio.PorNome).Select(i => i.Produto.Nome);

        Assert.Equal(new[] { "alfa", "Mega", "Zeta" }, nomes);
    }

    [Fact]
    public void Data_Da_Ultima_Movimentacao_E_Nula_Sem_Movimentacoes()
    {
        Assert.Null(_c.Relatorios.DataDaUltimaMovimentacao());
    }

    [Fact]
    public void Data_Da_Ultima_Movimentacao_Considera_Qualquer_Status()
    {
        var produto = _c.NovoProduto();
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 1, new DateTime(2026, 5, 1));
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Saida, 1, new DateTime(2026, 9, 20, 14, 30, 0), StatusMovimentacao.Cancelada);
        _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 7, 3));

        Assert.Equal(new DateTime(2026, 9, 20, 14, 30, 0), _c.Relatorios.DataDaUltimaMovimentacao());
    }

    [Fact]
    public void Consumo_Com_Datas_Invertidas_E_Recusado()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _c.Relatorios.GerarRelatorioConsumo(Fim, Inicio, null, OrdemRelatorio.PorNome));
    }
}
