using SigaCean.Data;
using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Tests;

// Testa o histórico real do CEAN (Data/HistoricoCean.cs): as regras de leitura das linhas da planilha e os dados reais.
// O histórico é SÓ REGISTRO: nenhum teste aqui pode ver o estoque dos produtos mudar por causa dele.
public class HistoricoCeanTests
{
    private static Produto NovoProduto(string nome = "PRODUTO X") => new() { Nome = nome, Categoria = Categoria.Medicamento };

    private static LinhaHistoricoCean Linha(bool entrada, int qtd, string obs, string data = "2026-03-10", string produto = "PRODUTO X") =>
        new(data, entrada, produto, qtd, obs);

    private static Movimentacao Unica(ResultadoImportacao r) => Assert.Single(r.Movimentacoes);

    // Um AppData com os produtos do CEAN e o histórico carregados (do jeito que o MauiProgram faz).
    private static (AppData Dados, ResultadoImportacao Resultado) Carregado()
    {
        var dados = new AppData();
        DadosCean.Carregar(dados);
        var resultado = HistoricoCean.Carregar(dados);
        return (dados, resultado);
    }

    // ---- Regras de interpretação (linhas sintéticas) ----

    [Fact]
    public void Balanco_ViraAjusteDeBalanco_ComSaldoAnteriorEApurado()
    {
        var p = NovoProduto();
        var r = HistoricoCean.Importar([p],
        [
            Linha(true, 100, "BALANÇO"),
            Linha(false, 30, "BALANÇO"),
            Linha(true, 0, "BALANÇO")
        ]);

        var ajustes = r.Movimentacoes.ToList();
        Assert.All(ajustes, m => Assert.Equal(TipoMovimentacao.AjusteBalanco, m.Tipo));
        Assert.Equal((0, 100, 100), (ajustes[0].QuantidadeAnterior, ajustes[0].QuantidadeApurada, ajustes[0].Quantidade));
        Assert.Equal((100, 70, -30), (ajustes[1].QuantidadeAnterior, ajustes[1].QuantidadeApurada, ajustes[1].Quantidade));
        Assert.Equal((70, 70, 0), (ajustes[2].QuantidadeAnterior, ajustes[2].QuantidadeApurada, ajustes[2].Quantidade));
        Assert.Equal(70, r.SaldoDoHistorico[p]);
    }

    [Fact]
    public void AjusteParaBaixo_GuardaDeOndeSaiu_ParaOCancelamentoDevolver()
    {
        var r = HistoricoCean.Importar([NovoProduto()], [Linha(true, 100, "BALANÇO"), Linha(false, 30, "BALANÇO")]);

        Assert.Empty(r.Movimentacoes[0].SaldosConsumidos);
        var consumido = Assert.Single(r.Movimentacoes[1].SaldosConsumidos);
        Assert.Equal((null, 30), (consumido.DataValidade, consumido.Quantidade));
    }

    [Fact]
    public void EntradaDeCompra_GuardaTipoENumeroDaAF()
    {
        var r = HistoricoCean.Importar([NovoProduto()], [Linha(true, 5, "COMPRA/LICITAÇÃO; AF 002000/2025")]);

        var e = Unica(r);
        Assert.Equal(TipoMovimentacao.Entrada, e.Tipo);
        Assert.Equal(TipoOrigemEntrada.Compra, e.TipoOrigem);
        Assert.Equal("002000/2025", e.NumeroAF);
        Assert.Null(e.Origem);
    }

    [Fact]
    public void EntradaDeCompra_ComAFNaoPadrao_GuardaOTextoComoDetalhe()
    {
        var r = HistoricoCean.Importar([NovoProduto()], [Linha(true, 5, "COMPRA/LICITAÇÃO; AF, ALC/2026")]);

        var e = Unica(r);
        Assert.Equal(TipoOrigemEntrada.Compra, e.TipoOrigem);
        Assert.Null(e.NumeroAF);
        Assert.Equal("AF, ALC/2026", e.Origem);
    }

    [Theory]
    [InlineData("DOAÇÃO", null)]
    [InlineData("DOAÇÃO; DR WAGNER", "DR WAGNER")]
    public void EntradaDeDoacao_GuardaTipoEDoador(string obs, string? doador)
    {
        var e = Unica(HistoricoCean.Importar([NovoProduto()], [Linha(true, 5, obs)]));

        Assert.Equal(TipoOrigemEntrada.Doacao, e.TipoOrigem);
        Assert.Equal(doador, e.Origem);
    }

    [Fact]
    public void EntradaSemOrigemConhecida_FicaSemTipo_NaoInventaCompraNemDoacao()
    {
        var r = HistoricoCean.Importar([NovoProduto()],
        [
            Linha(true, 5, ""),
            Linha(true, 5, "SANTA CASA")
        ]);

        var entradas = r.Movimentacoes.ToList();
        Assert.All(entradas, e => Assert.Null(e.TipoOrigem));
        Assert.Null(entradas[0].Origem);
        Assert.Equal("SANTA CASA", entradas[1].Origem);
    }

    [Theory]
    [InlineData("CEAN; CANIL", "CEAN; CANIL")]
    [InlineData("", "Não informado")]
    [InlineData("DOAÇÃO PARA MUNICIPE", "DOAÇÃO PARA MUNICIPE")]
    public void Saida_GuardaODestino_OuNaoInformado(string obs, string destinoEsperado)
    {
        var s = Unica(HistoricoCean.Importar([NovoProduto()], [Linha(false, 3, obs)]));

        Assert.Equal(TipoMovimentacao.Saida, s.Tipo);
        Assert.Equal(destinoEsperado, s.Destino);
        Assert.Equal(3, s.Quantidade);
        Assert.Null(s.RetiradoPor); // a planilha não diz quem retirou
    }

    [Fact]
    public void Importar_NaoAlteraOEstoqueDosProdutos()
    {
        var p = NovoProduto();
        p.Saldos.Add(new SaldoValidade { DataValidade = null, Quantidade = 10 });

        HistoricoCean.Importar([p], [Linha(true, 500, "BALANÇO"), Linha(false, 4, "CEAN; CANIL")]);

        Assert.Equal(10, p.Quantidade);
    }

    [Fact]
    public void Importar_GuardaDataDaPlanilha_ESemUsuarioResponsavel()
    {
        var s = Unica(HistoricoCean.Importar([NovoProduto()], [Linha(false, 1, "", data: "2026-07-21")]));

        Assert.Equal(new DateTime(2026, 7, 21), s.Data);
        Assert.Null(s.UsuarioResponsavel);
        Assert.Equal(StatusMovimentacao.Ativa, s.Status);
    }

    [Fact]
    public void ProdutoQueNaoExiste_VaiParaSemProduto_ENaoQuebra()
    {
        var r = HistoricoCean.Importar([NovoProduto()], [Linha(false, 1, "", produto: "NAO EXISTE")]);

        Assert.Empty(r.Movimentacoes);
        Assert.Single(r.SemProduto);
    }

    [Fact]
    public void NomeComErroDeDigitacao_UsaAEquivalenciaConhecida()
    {
        var p = NovoProduto("AURITOP, GEL OTOLÓGICO 30 ML");

        var r = HistoricoCean.Importar([p], [Linha(false, 1, "", produto: "AURITOP, GEO OTOLÓGICO 30 ML")]);

        Assert.Same(p, Assert.Single(r.Movimentacoes).Produto);
    }

    // ---- Dados reais da planilha ----

    [Fact]
    public void Equivalencias_ApontamDeUmNomeDoHistoricoParaUmProdutoQueExiste()
    {
        var (dados, _) = Carregado();
        var nomesDoHistorico = HistoricoCean.Linhas.Select(l => l.Produto).ToHashSet();
        var nomesDeProdutos = dados.Produtos.Select(p => p.Nome).ToHashSet();

        foreach (var (origem, destino) in HistoricoCean.Equivalencias)
        {
            Assert.Contains(origem, nomesDoHistorico);
            Assert.Contains(destino, nomesDeProdutos);
        }
    }

    [Fact]
    public void HistoricoReal_TemAsLinhasDaPlanilha_ComDatasEQuantidadesValidas()
    {
        Assert.Equal(737, HistoricoCean.Linhas.Length);
        Assert.All(HistoricoCean.Linhas, l =>
        {
            Assert.True(DateOnly.TryParseExact(l.Data, "yyyy-MM-dd", out _), $"data inválida: {l.Data}");
            Assert.True(l.Quantidade >= 0, $"quantidade negativa: {l.Produto}");
            Assert.False(string.IsNullOrWhiteSpace(l.Produto));
        });
    }

    [Fact]
    public void HistoricoReal_NomesSemProdutoCorrespondente_SaoOsNoveConhecidos()
    {
        var (_, r) = Carregado();

        var semProduto = r.SemProduto.Select(l => l.Produto).Distinct().Order().ToList();
        Assert.Equal(9, semProduto.Count);
        Assert.Equal(24, r.SemProduto.Count);
        Assert.Contains("XILAZINA", semProduto);
        Assert.Contains("SULFATO DE MORFINA 10 MG - INJETÁVEL - AMPOLA", semProduto);
        Assert.Contains(semProduto, n => n.StartsWith("DEXAMETASONA 200 MG, DEXAFLAN"));
        Assert.Contains(semProduto, n => n.StartsWith("AMOXICILINA TRIHIDRATADA, AGEMOXI - 17 MG"));
        Assert.Contains(semProduto, n => n.StartsWith("CARPROFENO, IBASA"));
        Assert.Contains(semProduto, n => n.StartsWith("ADVOCATE, IMIDACLOPRIDA"));
        Assert.Equal(HistoricoCean.Linhas.Length, r.Movimentacoes.Count + r.SemProduto.Count);
    }

    [Fact]
    public void HistoricoReal_SeteProdutosTemSaldoDiferenteDoEstoqueDaPlanilha()
    {
        var (dados, r) = Carregado();

        var divergentes = dados.Produtos
            .Where(p => r.SaldoDoHistorico.GetValueOrDefault(p) != p.Quantidade)
            .Select(p => p.Nome)
            .ToList();

        Assert.Equal(7, divergentes.Count);
        Assert.Contains("CLORETO DE POTÁSSIO 19,1% - INJETÁVEL, AMPOLA", divergentes);
        Assert.Contains("TACROLIMO, TARFIC 0,1", divergentes);
    }

    [Fact]
    public void Carregar_AcrescentaOHistorico_SemMexerNoEstoque()
    {
        var antes = new AppData();
        DadosCean.Carregar(antes);
        var estoqueAntes = antes.Produtos.ToDictionary(p => p.Nome, p => Cenario.Foto(p));

        var (dados, _) = Carregado();

        Assert.Equal(EsperadoImportadas, dados.Movimentacoes.Count);
        Assert.Equal(4878, dados.Produtos.Sum(p => p.Quantidade));
        Assert.All(dados.Produtos, p => Assert.Equal(estoqueAntes[p.Nome], Cenario.Foto(p)));
        Assert.All(dados.Movimentacoes, m => Assert.Equal(StatusMovimentacao.Ativa, m.Status));
    }

    [Fact]
    public void AppDataNovo_ContinuaVazio_OHistoricoSoEntraQuandoOMauiProgramPede()
    {
        Assert.Empty(new AppData().Movimentacoes);
    }

    [Fact]
    public void HistoricoReal_TiposDeMovimentacao()
    {
        var (dados, _) = Carregado();

        Assert.Equal(EsperadoAjustes, dados.Movimentacoes.Count(m => m.Tipo == TipoMovimentacao.AjusteBalanco));
        Assert.Equal(EsperadoEntradas, dados.Movimentacoes.Count(m => m.Tipo == TipoMovimentacao.Entrada));
        Assert.Equal(EsperadoSaidas, dados.Movimentacoes.Count(m => m.Tipo == TipoMovimentacao.Saida));
    }

    [Fact]
    public void HistoricoReal_PrimeiraMovimentacao_EOBalancoInicialDoPrador()
    {
        var (dados, _) = Carregado();

        var primeira = dados.Movimentacoes.First();
        Assert.Equal(TipoMovimentacao.AjusteBalanco, primeira.Tipo);
        Assert.Equal("PRADOR, MELOXICAM 1 MG + DIPIRONA 50 MG - INJETÁVEL, FRASCO 100 ML", primeira.Produto.Nome);
        Assert.Equal(new DateTime(2026, 1, 20), primeira.Data);
        Assert.Equal((0, 6), (primeira.QuantidadeAnterior, primeira.QuantidadeApurada));
    }

    [Fact]
    public void HistoricoReal_CompraComAF_FoiInterpretada()
    {
        var (dados, _) = Carregado();

        var compras = dados.Movimentacoes.Where(m => m.Tipo == TipoMovimentacao.Entrada && m.TipoOrigem == TipoOrigemEntrada.Compra).ToList();
        Assert.Equal(19, compras.Count);
        Assert.Equal(18, compras.Count(e => e.NumeroAF is not null));
        Assert.Contains(compras, e => e.NumeroAF == "002000/2025");
    }

    [Fact]
    public void CancelarSaidaImportada_DevolveAQuantidadeAoEstoque()
    {
        var (dados, _) = Carregado();
        var estoque = new EstoqueService(dados);
        var movimentacoes = new MovimentacaoService(dados, estoque);
        var saida = dados.Movimentacoes.First(m => m.Tipo == TipoMovimentacao.Saida && m.Quantidade > 0);
        var antes = saida.Produto.Quantidade;

        movimentacoes.Cancelar(saida.Id, "Teste de cancelamento");

        Assert.Equal(antes + saida.Quantidade, saida.Produto.Quantidade);
        Assert.Equal(StatusMovimentacao.Cancelada, saida.Status);
    }

    [Fact]
    public void Relatorio_UsaOHistoricoImportado()
    {
        var (dados, _) = Carregado();
        var relatorio = new RelatorioService(dados, new EstoqueService(dados));

        var itens = relatorio.GerarRelatorioConsumo(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.True(itens.Count > 50);
        Assert.Equal(dados.Movimentacoes.Where(m => m.Tipo == TipoMovimentacao.Saida).Sum(m => m.Quantidade), itens.Sum(i => i.TotalSaidas));
    }

    [Fact]
    public void Historico_AparecePeloMovimentacaoService_DoMaisRecenteParaOMaisAntigo()
    {
        var (dados, _) = Carregado();
        var movimentacoes = new MovimentacaoService(dados, new EstoqueService(dados));

        var historico = movimentacoes.ConsultarHistorico();

        Assert.Equal(EsperadoImportadas, historico.Count);
        Assert.True(historico[0].Data >= historico[^1].Data);
    }

    // Valores conferidos contra a planilha original (mesmos do projeto completo): 737 linhas válidas, 24 sem produto correspondente.
    private const int EsperadoImportadas = 713;
    private const int EsperadoAjustes = 218;
    private const int EsperadoEntradas = 100;
    private const int EsperadoSaidas = 395;
}
