namespace SigaCean.Models;

// Uma linha do "Consumo por categoria": como a categoria se comportou no período. Quem monta é o RelatorioService.
// Só conta produto ATIVO. "Uso" = só SAÍDAS com status Ativa (entradas, ajustes, corrigidas e canceladas não contam).
public class ConsumoPorCategoria
{
    public Categoria Categoria { get; init; }
    public int QuantidadeDeProdutos { get; init; } // produtos ativos da categoria
    public int EstoqueAtual { get; init; } // soma das quantidades desses produtos
    public int TotalSaidas { get; init; }
    public ConsumoDeProduto? MaisUsado { get; init; } // nulo se nenhum produto teve saída
    public ConsumoDeProduto? MenosUsado { get; init; } // nulo se menos de dois produtos tiveram saída
    public int ProdutosSemSaida { get; init; } // ativos sem nenhuma saída no período (nunca são "menos usados")
    public List<ConsumoDeProduto> ProdutosUsados { get; init; } = new(); // do mais ao menos usado; empate por nome
}