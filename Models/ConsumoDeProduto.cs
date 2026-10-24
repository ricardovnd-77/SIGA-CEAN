namespace SigaCean.Models;

// Quanto um produto saiu no período (só saídas ativas contam). Usado no Consumo por categoria.
public record ConsumoDeProduto(Produto Produto, int TotalSaidas);