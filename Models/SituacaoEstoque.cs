namespace SigaCean.Models;

// Situação da QUANTIDADE de um produto em relação ao mínimo. Quem calcula é o EstoqueService.
// (A situação da VALIDADE é separada: veja EstoqueService.TemValidadeVencida / TemValidadeProxima.)
public enum SituacaoEstoque
{
    Normal,
    AbaixoDoMinimo, // quantidade <= mínimo (inclui estar exatamente no mínimo)
    SemEstoque // quantidade == 0
}
