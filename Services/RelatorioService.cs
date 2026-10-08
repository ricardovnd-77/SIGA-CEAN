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

    // TODO: por produto, no período: total de entradas, de saídas, de ajustes e o estoque atual.
    // Filtro opcional por categoria e ordenação por maior consumo (saídas). O dia final entra inteiro.
    public List<ItemRelatorioConsumo> GerarRelatorioConsumo(DateTime inicio, DateTime fim, Categoria? categoria = null)
    {
        throw new NotImplementedException("TODO: RelatorioService.GerarRelatorioConsumo");
    }

    // TODO: entradas do período.
    public List<Movimentacao> ObterEntradas(DateTime inicio, DateTime fim)
    {
        throw new NotImplementedException("TODO: RelatorioService.ObterEntradas");
    }

    // TODO: saídas do período.
    public List<Movimentacao> ObterSaidas(DateTime inicio, DateTime fim)
    {
        throw new NotImplementedException("TODO: RelatorioService.ObterSaidas");
    }

    // TODO: estoque disponível hoje (produto e quantidade).
    public List<Produto> ObterEstoqueDisponivel()
    {
        throw new NotImplementedException("TODO: RelatorioService.ObterEstoqueDisponivel");
    }

    // TODO: produtos com estoque baixo (use EstoqueService.EstaAbaixoDoMinimo).
    public List<Produto> ObterProdutosComEstoqueBaixo()
    {
        throw new NotImplementedException("TODO: RelatorioService.ObterProdutosComEstoqueBaixo");
    }

    // TODO: produtos próximos da validade (use EstoqueService.TemValidadeProxima).
    public List<Produto> ObterProdutosProximosDaValidade()
    {
        throw new NotImplementedException("TODO: RelatorioService.ObterProdutosProximosDaValidade");
    }
}
