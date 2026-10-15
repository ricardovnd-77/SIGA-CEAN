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
    // Ordenado por maior consumo (saídas) e, em empate, por nome.
    public List<ItemRelatorioConsumo> GerarRelatorioConsumo(DateTime inicio, DateTime fim, Categoria? categoria = null)
    {
        var movimentacoes = ObterAtivasNoPeriodo(inicio, fim);

        if (categoria is not null)
            movimentacoes = movimentacoes.Where(m => m.Produto.Categoria == categoria).ToList();

        return movimentacoes
            .GroupBy(m => m.Produto)
            .Select(grupo => new ItemRelatorioConsumo
            {
                Produto = grupo.Key,
                TotalEntradas = SomarQuantidades(grupo, TipoMovimentacao.Entrada),
                TotalSaidas = SomarQuantidades(grupo, TipoMovimentacao.Saida),
                TotalAjustes = SomarQuantidades(grupo, TipoMovimentacao.AjusteBalanco), // soma das diferenças, com sinal
                EstoqueAtual = grupo.Key.Quantidade
            })
            .OrderByDescending(item => item.TotalSaidas)
            .ThenBy(item => item.Produto.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

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