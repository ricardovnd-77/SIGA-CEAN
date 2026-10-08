namespace SigaCean.Models;

// Uma linha do Relatório de Consumo: o que aconteceu com um produto no período. Quem monta é o RelatorioService.
public class ItemRelatorioConsumo
{
    public required Produto Produto { get; init; }
    public int TotalEntradas { get; set; }
    public int TotalSaidas { get; set; }
    public int TotalAjustes { get; set; } // soma das diferenças dos ajustes de balanço
    public int EstoqueAtual { get; set; }
}
