namespace SigaCean.Models;

public enum TipoMovimentacao
{
    Entrada,
    Saida,

    // Correção feita depois de uma conferência física do estoque (na planilha real do CEAN aparece como "BALANÇO").
    // Não é uma entrada nem uma saída de uso normal, por isso tem tipo próprio.
    AjusteBalanco
}
