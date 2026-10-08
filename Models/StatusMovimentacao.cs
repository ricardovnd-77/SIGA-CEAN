namespace SigaCean.Models;

// Uma movimentação corrigida ou cancelada NÃO é apagada: continua no histórico, com o status e o motivo.
public enum StatusMovimentacao
{
    Ativa,
    Corrigida,
    Cancelada
}
