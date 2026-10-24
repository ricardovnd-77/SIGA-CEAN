using SigaCean.Data;
using SigaCean.Models;

namespace SigaCean.Services;

// Relatórios. O mais importante para o CEAN é o de CONSUMO (foi o problema nº 1 que eles apontaram).
// Use o EstoqueService para as situações (não repita a regra de estoque baixo ou de validade aqui).
// Só movimentações com Status = Ativa contam (as corrigidas e as canceladas tiveram o efeito desfeito).
public class RelatorioService
{
    private readonly AppData _dados;
    private readonly EstoqueService _estoque;

    public RelatorioService(AppData dados, EstoqueService estoque)
    {
        _dados = dados;
        _estoque = estoque;
    }

    // Uma linha por produto que teve movimentação no período. Filtro opcional por categoria.
    // Ordem padrão: maior consumo (saídas) e, em empate, nome. Com OrdemRelatorio.PorNome: só por nome.
    // MediaMensalSaidas = saídas / meses do período (PeriodoRelatorio.MesesNoPeriodo), com 1 casa decimal.
    public List<ItemRelatorioConsumo> GerarRelatorioConsumo(DateTime inicio, DateTime fim, Categoria? categoria = null,
        OrdemRelatorio ordem = OrdemRelatorio.MaiorConsumo)
    {
        var movimentacoes = ObterAtivasNoPeriodo(inicio, fim);
        var meses = PeriodoRelatorio.MesesNoPeriodo(inicio, fim);

        if (categoria is not null)
            movimentacoes = movimentacoes.Where(m => m.Produto.Categoria == categoria).ToList();

        var itens = movimentacoes
            .GroupBy(m => m.Produto)
            .Select(grupo =>
            {
                var saidas = SomarQuantidades(grupo, TipoMovimentacao.Saida);
                return new ItemRelatorioConsumo
                {
                    Produto = grupo.Key,
                    TotalEntradas = SomarQuantidades(grupo, TipoMovimentacao.Entrada),
                    TotalSaidas = saidas,
                    TotalAjustes = SomarQuantidades(grupo, TipoMovimentacao.AjusteBalanco), // soma das diferenças, com sinal
                    EstoqueAtual = grupo.Key.Quantidade,
                    MediaMensalSaidas = Math.Round(saidas / meses, 1)
                };
            });

        var ordenados = ordem == OrdemRelatorio.PorNome
            ? itens.OrderBy(item => item.Produto.Nome, StringComparer.CurrentCultureIgnoreCase)
            : itens.OrderByDescending(item => item.TotalSaidas)
                   .ThenBy(item => item.Produto.Nome, StringComparer.CurrentCultureIgnoreCase);

        return ordenados.ToList();
    }

    // Consumo por categoria: UMA linha para CADA categoria (mesmo sem produto), na ordem do enum.
    // Só conta produto ATIVO e só SAÍDAS ativas do período (mesmas regras de período do relatório de consumo).
    public List<ConsumoPorCategoria> GerarConsumoPorCategoria(DateTime inicio, DateTime fim)
    {
        var saidasPorProduto = ObterAtivasDoTipo(inicio, fim, TipoMovimentacao.Saida)
            .GroupBy(m => m.Produto)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(m => m.Quantidade));

        var resultado = new List<ConsumoPorCategoria>();

        foreach (var categoria in Enum.GetValues<Categoria>())
        {
            var ativos = _dados.Produtos.Where(p => p.Ativo && p.Categoria == categoria).ToList();

            var usados = ativos
                .Where(p => saidasPorProduto.TryGetValue(p, out var total) && total > 0)
                .Select(p => new ConsumoDeProduto(p, saidasPorProduto[p]))
                .OrderByDescending(c => c.TotalSaidas)
                .ThenBy(c => c.Produto.Nome, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            resultado.Add(new ConsumoPorCategoria
            {
                Categoria = categoria,
                QuantidadeDeProdutos = ativos.Count,
                EstoqueAtual = ativos.Sum(p => p.Quantidade),
                TotalSaidas = usados.Sum(c => c.TotalSaidas),
                MaisUsado = usados.FirstOrDefault(),
                MenosUsado = usados.Count >= 2 ? usados[^1] : null, // com um só produto usado não há "menos usado"
                ProdutosSemSaida = ativos.Count - usados.Count,
                ProdutosUsados = usados
            });
        }

        return resultado;
    }

    // Data da movimentação mais recente (de qualquer status); nula se ainda não há movimentações.
    // Serve para orientar a tela quando o período escolhido está vazio.
    public DateTime? DataDaUltimaMovimentacao() =>
        _dados.Movimentacoes.Count == 0 ? null : _dados.Movimentacoes.Max(m => m.Data);

    // Entradas do período, da mais recente para a mais antiga.
    public List<Movimentacao> ObterEntradas(DateTime inicio, DateTime fim) =>
        ObterAtivasDoTipo(inicio, fim, TipoMovimentacao.Entrada);

    // Saídas do período, da mais recente para a mais antiga.
    public List<Movimentacao> ObterSaidas(DateTime inicio, DateTime fim) =>
        ObterAtivasDoTipo(inicio, fim, TipoMovimentacao.Saida);

    // Estoque disponível hoje: produtos ativos com quantidade maior que zero, por nome.
    public List<Produto> ObterEstoqueDisponivel() =>
        OrdenarPorNome(_dados.Produtos.Where(p => p.Ativo && p.Quantidade > 0));

    // A regra de estoque baixo é do EstoqueService.
    public List<Produto> ObterProdutosComEstoqueBaixo() =>
        OrdenarPorNome(_dados.Produtos.Where(p => p.Ativo && _estoque.EstaAbaixoDoMinimo(p)));

    // A regra de validade próxima é do EstoqueService (aqui só se informa a data de hoje).
    public List<Produto> ObterProdutosProximosDaValidade()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        return OrdenarPorNome(_dados.Produtos.Where(p => p.Ativo && _estoque.TemValidadeProxima(p, hoje)));
    }

    // ---------- Apoio (privados) ----------

    // Movimentações ATIVAS do período (mesmas regras do FiltrarPorPeriodo: início depois do fim é erro
    // e o dia final entra inteiro). Corrigidas e canceladas ficam de fora.
    private List<Movimentacao> ObterAtivasNoPeriodo(DateTime inicio, DateTime fim)
    {
        if (inicio > fim)
            throw new InvalidOperationException("A data inicial não pode ser depois da data final.");

        var limite = fim.Date.AddDays(1).AddTicks(-1);

        return _dados.Movimentacoes
            .Where(m => m.Status == StatusMovimentacao.Ativa && m.Data >= inicio && m.Data <= limite)
            .ToList();
    }

    private List<Movimentacao> ObterAtivasDoTipo(DateTime inicio, DateTime fim, TipoMovimentacao tipo) =>
        ObterAtivasNoPeriodo(inicio, fim)
            .Where(m => m.Tipo == tipo)
            .OrderByDescending(m => m.Data)
            .ToList();

    private static int SomarQuantidades(IEnumerable<Movimentacao> movimentacoes, TipoMovimentacao tipo) =>
        movimentacoes.Where(m => m.Tipo == tipo).Sum(m => m.Quantidade);

    private static List<Produto> OrdenarPorNome(IEnumerable<Produto> produtos) =>
        produtos.OrderBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
}